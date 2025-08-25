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
		private const string HOST = "localhost";
		private const int PORT = 1012;

		private Mock<ReconnectTcpClient> _tcpClientMock;
		private Mock<NetworkStreamWrapper> _networkStreamMock;

		private Queue<(int DelaySeconds, byte[] BufferResponse)> _readAsyncResponses;

		[TestInitialize]
		public void Initialize()
		{
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
		public void ShouldSetAndGetLogger()
		{
			// Arrange
			var loggerMock = new Mock<ILogger>();
			var client = GetClient();

			// Act
			client.Logger = loggerMock.Object;

			// Assert
			Assert.AreEqual(loggerMock.Object, client.Logger);

			_tcpClientMock.VerifySet(m => m.Logger = loggerMock.Object, Times.Once);
			_tcpClientMock.VerifyGet(m => m.IsConnected, Times.Once);
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldDisposeOnlyOnce()
		{
			// Arrange
			var client = GetClient();

			// Act
			client.Dispose();
			client.Dispose();

			// Assert
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);
			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

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
				.Setup(m => m.GetStream())
				.Returns(_networkStreamMock.Object);

			_networkStreamMock
				.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns<byte[], int, int, CancellationToken>((buffer, offset, count, token) =>
				{
					var (delaySeconds, bufferResponse) = _readAsyncResponses.Dequeue();

					return Task.Delay(TimeSpan.FromSeconds(delaySeconds), token).ContinueWith(t =>
					{
						Array.Copy(bufferResponse, 0, buffer, offset, bufferResponse.Length);
						return bufferResponse.Length;
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
