using System;
using AMWD.Net.Api.Fritz.CallMonitor;

namespace FritzCallMonitor.Tests
{
	[TestClass]
	public class CallMonitorEventArgsTest
	{
		private string _dateOffset;

		private readonly DateTime NOW = new(2025, 8, 25, 20, 15, 30, DateTimeKind.Local);

		[TestInitialize]
		public void Initialize()
		{
			var offset = TimeZoneInfo.Local.GetUtcOffset(NOW);
			_dateOffset = offset < TimeSpan.Zero
				? "-" + offset.ToString("hh\\:mm")
				: "+" + offset.ToString("hh\\:mm");
		}

		[TestMethod]
		public void ShouldParseRingEvent()
		{
			// Arrange
			string line = $"{NOW:dd.MM.yy HH:mm:ss};RING;2;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Ring, result.Event);
			Assert.AreEqual(2, result.ConnectionId);
			Assert.IsNull(result.LinePort);
			Assert.AreEqual("012345678901", result.ExternalNumber);
			Assert.AreEqual("9876543", result.InternalNumber);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldParseConnectEvent()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};CONNECT;1;3;012345678901;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Connect, result.Event);
			Assert.AreEqual(1, result.ConnectionId);
			Assert.AreEqual(3, result.LinePort);
			Assert.AreEqual("012345678901", result.ExternalNumber);
			Assert.IsNull(result.InternalNumber);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldParseDisconnectEvent()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};DISCONNECT;2;42;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Disconnect, result.Event);
			Assert.AreEqual(2, result.ConnectionId);
			Assert.IsNull(result.LinePort);
			Assert.IsNull(result.ExternalNumber);
			Assert.IsNull(result.InternalNumber);
			Assert.AreEqual(TimeSpan.FromSeconds(42), result.Duration);
		}

		[TestMethod]
		public void ShouldParseCallEvent()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};CALL;4;7;9876543;012345678901;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Call, result.Event);
			Assert.AreEqual(4, result.ConnectionId);
			Assert.AreEqual(7, result.LinePort);
			Assert.AreEqual("012345678901", result.ExternalNumber);
			Assert.AreEqual("9876543", result.InternalNumber);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldReturnNullOnInvalidDate()
		{
			string line = "99.99.99 99:99:99;RING;2;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldReturnNullOnUnknownEventType()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};UNKNOWN;2;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldReturnNullOnInvalidConnectionId()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};RING;abc;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldHandleInvalidLinePortInConnect()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};CONNECT;1;abc;012345678901;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.IsNull(result.LinePort);
		}

		[TestMethod]
		public void ShouldHandleInvalidLinePortInCall()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};CALL;4;abc;9876543;012345678901;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.IsNull(result.LinePort);
		}

		[TestMethod]
		public void ShouldHandleInvalidDurationInDisconnect()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};DISCONNECT;2;abc;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldReturnNullOnTooFewColumns()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};RING;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldParseWithExtraColumns()
		{
			string line = $"{NOW:dd.MM.yy HH:mm:ss};RING;2;012345678901;9876543;SIP0;EXTRA;COLUMN;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.AreEqual("012345678901", result.ExternalNumber);
			Assert.AreEqual("9876543", result.InternalNumber);
		}
	}
}
