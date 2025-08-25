using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Net.Api.Fritz.CallMonitor.Utils;
using AMWD.Net.Api.Fritz.CallMonitor.Wrappers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FritzCallMonitor.Tests
{
	[TestClass]
	public class ReconnectTcpClientTest
	{
		private const int ASYNC_DELAY = 10;

		public TestContext TestContext { get; set; }

		private const int PORT = 4711;
		private const string HOST = "localhost";

		private Mock<SocketWrapper> _socketMock;
		private Mock<TcpClientWrapper> _tcpClientMock;
		private Mock<NetworkStreamWrapper> _networkStreamMock;
		private Mock<TcpClientWrapperFactory> _tcpClientFactoryMock;

		private bool _tcpClientConnected;
		private Queue<int> _tcpClientConnectTaskDelays;

		private Queue<int> _networkStreamReadDelays;

		[TestInitialize]
		public void Initialize()
		{
			_tcpClientConnected = true;

			_tcpClientConnectTaskDelays = new Queue<int>();
			_networkStreamReadDelays = new Queue<int>();

			_tcpClientConnectTaskDelays.Enqueue(0);
			_networkStreamReadDelays.Enqueue(0);
		}

		[TestMethod]
		public void ShouldCreateInstance()
		{
			// Arrange

			// Act & Assert
			using var client = new ReconnectTcpClient(HOST, PORT);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("   ")]
		public void ShouldThrowArgumentNullExceptionOnMissingHost(string host)
		{
			// Arrange

			// Act & Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => new ReconnectTcpClient(host, PORT));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(65536)]
		public void ShouldThrowArgumentOutOfRangeExceptionOnInvalidPort(int port)
		{
			// Arrange

			// Act & Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ReconnectTcpClient(HOST, port));
		}

		[TestMethod]
		public async Task ShouldDispose()
		{
			// Arrange
			var client = GetClient();
			await client.StartAsync(TestContext.CancellationTokenSource.Token);

			// Act
			client.Dispose();

			// Assert
			_tcpClientMock.Verify(m => m.Dispose(), Times.Once);
			_tcpClientMock.VerifyGet(m => m.Client, Times.Once);
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldDisposeOnlyOnce()
		{
			// Arrange
			using var client = GetClient();
			await client.StartAsync(TestContext.CancellationTokenSource.Token);

			// Act
			client.Dispose();
			client.Dispose();

			// Assert
			_tcpClientMock.Verify(m => m.Dispose(), Times.Once);
			_tcpClientMock.VerifyGet(m => m.Client, Times.Once);
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldThrowObjectDisposedExceptionOnStart()
		{
			// Arrange
			using var client = GetClient();
			client.Dispose();

			// Act & Assert
			await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await client.StartAsync(TestContext.CancellationTokenSource.Token));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldThrowObjectDisposedExceptionOnStop()
		{
			// Arrange
			using var client = GetClient();
			client.Dispose();

			// Act & Assert
			await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await client.StopAsync(TestContext.CancellationTokenSource.Token));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldThrowObjectDisposedExceptionOnGetStream()
		{
			// Arrange
			using var client = GetClient();
			client.Dispose();

			// Act & Assert
			Assert.ThrowsExactly<ObjectDisposedException>(() => client.GetStream());

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIsConnected()
		{
			// Arrange
			_tcpClientConnectTaskDelays.Enqueue(Timeout.Infinite);

			var client = GetClient();
			await client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);

			// Act & Assert
			_tcpClientConnected = true;
			Assert.IsTrue(client.IsConnected);

			// Act & Assert
			_tcpClientConnected = false;
			Assert.IsFalse(client.IsConnected);

			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.AtLeastOnce);

			_tcpClientMock.VerifyGet(m => m.Client, Times.AtLeastOnce);
			_tcpClientMock.VerifyGet(m => m.Connected, Times.AtLeast(2));
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldCallOnConnectedCallback()
		{
			// Arrange
			var client = GetClient();
			bool callbackCalled = false;
			client.OnConnected = c =>
			{
				callbackCalled = true;
				return Task.CompletedTask;
			};

			// Act
			await client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			await client.StopAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);

			// Assert
			Assert.IsTrue(callbackCalled);

			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Once);

			_tcpClientMock.VerifyGet(m => m.Client, Times.Once);
			_tcpClientMock.VerifyGet(m => m.Connected, Times.Once);
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(m => m.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldStartAndStopWithoutException()
		{
			// Arrange
			var client = GetClient();

			// Act & Assert
			await client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			await client.StopAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);

			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Once);

			_tcpClientMock.VerifyGet(m => m.Client, Times.Once);
			_tcpClientMock.VerifyGet(m => m.Connected, Times.Once);
			_tcpClientMock.Verify(m => m.ConnectAsync("localhost", 4711, It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(m => m.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldStopWithoutStart()
		{
			// Arrange
			var client = GetClient();

			// Act & Assert
			await client.StopAsync(TestContext.CancellationTokenSource.Token);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnStream()
		{
			// Arrange
			var client = GetClient();
			await client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);

			// Act
			var stream = client.GetStream();

			// Assert
			Assert.IsNotNull(stream);
			Assert.AreEqual(_networkStreamMock.Object, stream);

			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Once);

			_tcpClientMock.VerifyGet(m => m.Client, Times.Once);
			_tcpClientMock.VerifyGet(m => m.Connected, Times.Once);
			_tcpClientMock.Verify(m => m.ConnectAsync("localhost", 4711, It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.Verify(m => m.GetStream(), Times.Exactly(2));

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldReturnNullStreamWhenNotConnected()
		{
			// Arrange
			var client = GetClient();

			// Act
			var stream = client.GetStream();

			// Assert
			Assert.IsNull(stream);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldTerminateCleanly()
		{
			// Arrange
			_tcpClientConnectTaskDelays.Clear();
			_tcpClientConnectTaskDelays.Enqueue(Timeout.Infinite);
			using var client = GetClient();

			// Act
			var startTask = client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			await client.StopAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			await startTask;

			// Assert
			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Once);

			_tcpClientMock.VerifyGet(m => m.Client, Times.Once);
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.Verify(m => m.Dispose(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public async Task ShouldLogConnectError(bool useLogger)
		{
			// Arrange
			var loggerMock = new Mock<ILogger>();

			using var client = GetClient();
			if (useLogger)
				client.Logger = loggerMock.Object;

			_tcpClientMock
				.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new SocketException());

			// Act
			var startTask = client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(1000, TestContext.CancellationTokenSource.Token); // Should try to connect two times.
			await client.StopAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			await startTask;

			// Assert
			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.Exactly(2));

			_tcpClientMock.VerifyGet(m => m.Client, Times.Exactly(2));
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.Exactly(2));
			_tcpClientMock.Verify(m => m.Dispose(), Times.Exactly(2));

			if (useLogger)
			{
				loggerMock.Verify(
					m => m.Log(LogLevel.Warning, It.IsAny<EventId>(),
						It.Is<It.IsAnyType>((v, t) => v.ToString().Equals($"Failed to connect to {HOST}:{PORT}. Retrying in 500ms...")),
						It.IsAny<SocketException>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()),
					Times.Once
				);
				loggerMock.Verify(
					m => m.Log(LogLevel.Warning, It.IsAny<EventId>(),
						It.Is<It.IsAnyType>((v, t) => v.ToString().Equals($"Failed to connect to {HOST}:{PORT}. Retrying in 1000ms...")),
						It.IsAny<SocketException>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()),
					Times.Once
				);
				loggerMock.VerifyNoOtherCalls();
			}

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldSkipReconnect()
		{
			// Arrange
			using var client = GetClient();

			_networkStreamMock
				.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new ObjectDisposedException("Test"));

			// Act
			var startTask = client.StartAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(1000, TestContext.CancellationTokenSource.Token); // Should try to connect two times.
			await client.StopAsync(TestContext.CancellationTokenSource.Token);
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			await startTask;

			// Assert
			_socketMock.Verify(m => m.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true), Times.AtLeastOnce);

			_tcpClientMock.VerifyGet(m => m.Client, Times.AtLeastOnce);
			_tcpClientMock.VerifyGet(m => m.Connected, Times.AtLeast(2));
			_tcpClientMock.Verify(m => m.ConnectAsync(HOST, PORT, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
			_tcpClientMock.Verify(m => m.GetStream(), Times.AtLeastOnce);
			_tcpClientMock.Verify(m => m.Dispose(), Times.AtLeastOnce);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		private void VerifyNoOtherCalls()
		{
			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		private ReconnectTcpClient GetClient()
		{
			_socketMock = new Mock<SocketWrapper>(null);
			_tcpClientMock = new Mock<TcpClientWrapper>();
			_networkStreamMock = new Mock<NetworkStreamWrapper>(null);
			_tcpClientFactoryMock = new Mock<TcpClientWrapperFactory>();

			_tcpClientMock
				.Setup(m => m.Connected)
				.Returns(() => _tcpClientConnected);

			_tcpClientMock
				.Setup(m => m.Client)
				.Returns(() => _socketMock.Object);

			_tcpClientMock
				.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns<string, int, CancellationToken>((_, __, ct) => Task.Delay(_tcpClientConnectTaskDelays.Dequeue(), ct));

			_tcpClientMock
				.Setup(m => m.GetStream())
				.Returns(() => _networkStreamMock.Object);

			_networkStreamMock
				.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns<byte[], int, int, CancellationToken>((_, __, count, ct) => Task.Delay(_networkStreamReadDelays.Dequeue(), ct).ContinueWith(t => count, ct));

			_tcpClientFactoryMock
				.Setup(m => m.Create())
				.Returns(() => _tcpClientMock.Object);

			var client = new ReconnectTcpClient(HOST, PORT);

			var factoryFieldInfo = client.GetType().GetField("_tcpClientFactory", BindingFlags.NonPublic | BindingFlags.Instance);
			factoryFieldInfo.SetValue(client, _tcpClientFactoryMock.Object);

			return client;
		}
	}
}
