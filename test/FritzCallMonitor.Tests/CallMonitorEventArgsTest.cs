using System;
using AMWD.Net.Api.Fritz.CallMonitor;

namespace FritzCallMonitor.Tests
{
	[TestClass]
	public class CallMonitorEventArgsTest
	{
		private string _dateOffset;

		[TestInitialize]
		public void Initialize()
		{
			var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
			_dateOffset = offset < TimeSpan.Zero
				? "-" + offset.ToString("hh\\:mm")
				: "+" + offset.ToString("hh\\:mm");
		}

		[TestMethod]
		public void ShouldParseRingEvent()
		{
			// Arrange
			string line = "25.08.25 20:15:30;RING;2;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Ring, result.Event);
			Assert.AreEqual(2, result.ConnectionId);
			Assert.IsNull(result.LinePort);
			Assert.AreEqual("012345678901", result.CallerNumber);
			Assert.AreEqual("9876543", result.CalleeNumber);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldParseConnectEvent()
		{
			string line = "25.08.25 20:15:30;CONNECT;1;3;012345678901;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Connect, result.Event);
			Assert.AreEqual(1, result.ConnectionId);
			Assert.AreEqual(3, result.LinePort);
			Assert.AreEqual("012345678901", result.CallerNumber);
			Assert.IsNull(result.CalleeNumber);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldParseDisconnectEvent()
		{
			string line = "25.08.25 20:15:30;DISCONNECT;2;42;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Disconnect, result.Event);
			Assert.AreEqual(2, result.ConnectionId);
			Assert.IsNull(result.LinePort);
			Assert.IsNull(result.CallerNumber);
			Assert.IsNull(result.CalleeNumber);
			Assert.AreEqual(TimeSpan.FromSeconds(42), result.Duration);
		}

		[TestMethod]
		public void ShouldParseCallEvent()
		{
			string line = "25.08.25 20:15:30;CALL;4;7;9876543;012345678901;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);

			Assert.IsNotNull(result);
			Assert.AreEqual($"2025-08-25 20:15:30 {_dateOffset}", result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss K"));
			Assert.AreEqual(EventType.Call, result.Event);
			Assert.AreEqual(4, result.ConnectionId);
			Assert.AreEqual(7, result.LinePort);
			Assert.AreEqual("012345678901", result.CallerNumber);
			Assert.AreEqual("9876543", result.CalleeNumber);
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
			string line = "25.08.25 20:15:30;UNKNOWN;2;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldReturnNullOnInvalidConnectionId()
		{
			string line = "25.08.25 20:15:30;RING;abc;012345678901;9876543;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldHandleInvalidLinePortInConnect()
		{
			string line = "25.08.25 20:15:30;CONNECT;1;abc;012345678901;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.IsNull(result.LinePort);
		}

		[TestMethod]
		public void ShouldHandleInvalidLinePortInCall()
		{
			string line = "25.08.25 20:15:30;CALL;4;abc;9876543;012345678901;SIP0;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.IsNull(result.LinePort);
		}

		[TestMethod]
		public void ShouldHandleInvalidDurationInDisconnect()
		{
			string line = "25.08.25 20:15:30;DISCONNECT;2;abc;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.IsNull(result.Duration);
		}

		[TestMethod]
		public void ShouldReturnNullOnTooFewColumns()
		{
			string line = "25.08.25 20:15:30;RING;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ShouldParseWithExtraColumns()
		{
			string line = "25.08.25 20:15:30;RING;2;012345678901;9876543;SIP0;EXTRA;COLUMN;";
			var result = CallMonitorEventArgs.Parse(line);
			Assert.IsNotNull(result);
			Assert.AreEqual("012345678901", result.CallerNumber);
			Assert.AreEqual("9876543", result.CalleeNumber);
		}
	}
}
