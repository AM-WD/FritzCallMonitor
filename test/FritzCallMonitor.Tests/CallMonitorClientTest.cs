using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Net.Api.Fritz.CallMonitor;
using AMWD.Net.Api.Fritz.CallMonitor.Utils;
using AMWD.Net.Api.Fritz.CallMonitor.Wrappers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FritzCallMonitor.Tests
{
	[TestClass]
	public class CallMonitorClientTest
	{
		private const int ASYNC_DELAY = 100;

		public TestContext TestContext { get; set; }

		private const string HOST = "localhost";
		private const int PORT = 1012;

		private string _dateOffset;


		private Mock<ReconnectTcpClient> _tcpClientMock;
		private Mock<NetworkStreamWrapper> _networkStreamMock;

		private bool _tcpClientConnected;
		private Queue<(int DelaySeconds, byte[] BufferResponse)> _readAsyncResponses;

		[TestInitialize]
		public void Initialize()
		{
			var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
			_dateOffset = offset < TimeSpan.Zero
				? "-" + offset.ToString("hh\\:mm")
				: "+" + offset.ToString("hh\\:mm");

			_tcpClientConnected = true;

			_readAsyncResponses = new Queue<(int, byte[])>();

			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes("25.08.25 20:15:30;RING;2;012345678901;9876543;SIP0;\r\n")));
			_readAsyncResponses.Enqueue((Timeout.Infinite, Array.Empty<byte>()));
		}

		[TestMethod]
		public void ShouldCreateInstance()
		{
			// Arrange & Act
			using var client = new CallMonitorClient(HOST, PORT);

			// Assert
			Assert.IsNotNull(client);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("   ")]
		public void ShouldThrowArgumentNullExceptionOnMissingHost(string host)
		{
			// Arrange, Act & Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => new CallMonitorClient(host, PORT));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(65536)]
		public void ShouldThrowArgumentOutOfRangeExceptionOnInvalidPort(int port)
		{
			// Arrange, Act & Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CallMonitorClient(HOST, port));
		}

		[TestMethod]
		public async Task ShouldSetAndGetLogger()
		{
			// Arrange
			var loggerMock = new Mock<ILogger>();
			var client = GetClient();
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);

			// Act
			client.Logger = loggerMock.Object;
			client.Dispose();

			// Assert
			Assert.AreEqual(loggerMock.Object, client.Logger);

			_tcpClientMock.VerifySet(m => m.Logger = loggerMock.Object, Times.Once);
			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.Exactly(2));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldDisposeOnlyOnce()
		{
			// Arrange
			var client = GetClient();
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);

			// Act
			client.Dispose();
			client.Dispose();

			// Assert
			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.AtMost(2));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldSkipTaskWhenStreamIsNull()
		{
			// Arrange
			var client = GetClient();
			_tcpClientMock.Setup(m => m.GetStream()).Returns((NetworkStreamWrapper)null);

			// Act
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			client.Dispose();

			// Assert
			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.AtMost(2));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadAndParseLine()
		{
			// Arrange
			bool eventRaised = false;
			CallMonitorEventArgs eventArgs = null;
			var client = GetClient();
			client.OnEvent += (s, e) =>
			{
				eventRaised = true;
				eventArgs = e;
			};

			// Act
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			client.Dispose();

			// Assert
			Assert.IsTrue(eventRaised);
			Assert.IsNotNull(eventArgs);

			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", eventArgs.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Ring, eventArgs.Event);
			Assert.AreEqual(2, eventArgs.ConnectionId);
			Assert.IsNull(eventArgs.LinePort);
			Assert.AreEqual("012345678901", eventArgs.CallerNumber);
			Assert.AreEqual("9876543", eventArgs.CalleeNumber);
			Assert.IsNull(eventArgs.Duration);

			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.Exactly(2));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadAndParseInMultipleReads()
		{
			// Arrange
			_readAsyncResponses.Clear();
			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes("25.08.25 20:15:30;RING;")));
			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes("2;012345678901;9876543;SIP0;\n")));
			_readAsyncResponses.Enqueue((Timeout.Infinite, Array.Empty<byte>()));

			bool eventRaised = false;
			CallMonitorEventArgs eventArgs = null;
			var client = GetClient();
			client.OnEvent += (s, e) =>
			{
				eventRaised = true;
				eventArgs = e;
			};

			// Act
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			client.Dispose();

			// Assert
			Assert.IsTrue(eventRaised);
			Assert.IsNotNull(eventArgs);

			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", eventArgs.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Ring, eventArgs.Event);
			Assert.AreEqual(2, eventArgs.ConnectionId);
			Assert.IsNull(eventArgs.LinePort);
			Assert.AreEqual("012345678901", eventArgs.CallerNumber);
			Assert.AreEqual("9876543", eventArgs.CalleeNumber);
			Assert.IsNull(eventArgs.Duration);

			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.Exactly(3));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadAndParseMultipleEvents()
		{
			// Arrange
			_readAsyncResponses.Clear();
			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes("25.08.25 20:15:30;RING;2;012345678901;9876543;SIP0;\n25.08.25 20:15:30")));
			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes(";RING;2;012345678901;9876543;SIP0;\r\n")));
			_readAsyncResponses.Enqueue((Timeout.Infinite, Array.Empty<byte>()));

			int eventsRaised = 0;
			var client = GetClient();
			client.OnEvent += (s, e) =>
			{
				Interlocked.Increment(ref eventsRaised);
			};

			// Act
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			client.Dispose();

			// Assert
			Assert.AreEqual(2, eventsRaised);

			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.Exactly(3));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadAndParseMultipleEventsWithOneError()
		{
			// Arrange
			_readAsyncResponses.Clear();
			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes("25.08.25 20:15:30;TEST;2;012345678901;9876543;SIP0;\n25.08.25 20:15:30")));
			_readAsyncResponses.Enqueue((0, Encoding.UTF8.GetBytes(";RING;2;012345678901;9876543;SIP0;\r\n")));
			_readAsyncResponses.Enqueue((Timeout.Infinite, Array.Empty<byte>()));

			int eventsRaised = 0;
			var client = GetClient();
			client.OnEvent += (s, e) =>
			{
				Interlocked.Increment(ref eventsRaised);
			};

			// Act
			await Task.Delay(ASYNC_DELAY, TestContext.CancellationTokenSource.Token);
			client.Dispose();

			// Assert
			Assert.AreEqual(1, eventsRaised);

			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.Exactly(3));
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		private void VerifyNoOtherCalls()
		{
			_tcpClientMock.Verify(m => m.OnConnected, Times.Once);
			_tcpClientMock.VerifyNoOtherCalls();
		}

		private CallMonitorClient GetClient()
		{
			_tcpClientMock = new Mock<ReconnectTcpClient>(HOST, PORT);
			_networkStreamMock = new Mock<NetworkStreamWrapper>(null);

			_tcpClientMock
				.Setup(m => m.IsConnected)
				.Returns(() => _tcpClientConnected);

			_tcpClientMock
				.Setup(m => m.GetStream())
				.Returns(_networkStreamMock.Object);

			_networkStreamMock
				.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns<byte[], int, int, CancellationToken>((buffer, offset, count, token) =>
				{
					var (delaySeconds, bufferResponse) = _readAsyncResponses.Dequeue();

					return Task.Delay(TimeSpan.FromSeconds(delaySeconds), token).ContinueWith(t =>
					{
						int bytesToCopy = Math.Min(count, bufferResponse.Length - offset);

						Array.Copy(bufferResponse, 0, buffer, offset, bytesToCopy);
						return bytesToCopy;
					});
				});

			var client = new CallMonitorClient(HOST, PORT);
			var tcpClientField = client.GetType().GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance);

			((IDisposable)tcpClientField.GetValue(client)).Dispose();
			tcpClientField.SetValue(client, _tcpClientMock.Object);

			var onConnectedMethodInfo = client.GetType().GetMethod("OnConnected", BindingFlags.NonPublic | BindingFlags.Instance);
			_tcpClientMock.SetupGet(c => c.OnConnected).Returns((Func<ReconnectTcpClient, Task>)onConnectedMethodInfo.CreateDelegate(typeof(Func<ReconnectTcpClient, Task>), client));

			_tcpClientMock.Object.OnConnected(_tcpClientMock.Object).Wait();
			return client;
		}
	}
}
