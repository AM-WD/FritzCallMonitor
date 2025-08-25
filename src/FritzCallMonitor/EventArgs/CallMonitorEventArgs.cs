using System;
using System.Globalization;

namespace AMWD.Net.Api.Fritz.CallMonitor
{
	/// <summary>
	/// Provides data for call monitoring events, including details about the call type, identifiers, and optional metadata.
	/// </summary>
	public class CallMonitorEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the timestamp of the event.
		/// </summary>
		public DateTimeOffset? Timestamp { get; private set; }

		/// <summary>
		/// Gets the type of event.
		/// </summary>
		public EventType? Event { get; private set; }

		/// <summary>
		/// Gets the connection ID.
		/// </summary>
		public int? ConnectionId { get; private set; }

		/// <summary>
		/// Gets the line / port of signaled.
		/// </summary>
		public int? LinePort { get; private set; }

		/// <summary>
		/// Gets the external number displayed in the FRITZ!Box.
		/// </summary>
		public string? CallerNumber { get; private set; }

		/// <summary>
		/// Gets the internal number registered in the FRITZ!Box.
		/// </summary>
		public string? CalleeNumber { get; private set; }

		/// <summary>
		/// Gets the duarion of the call (only on <see cref="EventType.Disconnect"/> event).
		/// </summary>
		public TimeSpan? Duration { get; private set; }

		/// <summary>
		/// Tries to parse a line from the call monitor output into a <see cref="CallMonitorEventArgs"/> instance.
		/// </summary>
		/// <param name="line">The line from the call monitor output.</param>
		/// <returns><see langword="null"/> when parsing fails, otherwise a new instance of the <see cref="CallMonitorEventArgs"/>.</returns>
		internal static CallMonitorEventArgs? Parse(string line)
		{
			string[] columns = line.Trim().Split(';');

			if (!DateTimeOffset.TryParseExact(columns[0], "dd.MM.yy HH:mm:ss", null, DateTimeStyles.None, out var timestamp))
				return null;

			if (!Enum.TryParse<EventType>(columns[1], true, out var eventType))
				return null;

			if (!int.TryParse(columns[2], out int connectionId))
				return null;

			var args = new CallMonitorEventArgs
			{
				Timestamp = timestamp,
				Event = eventType,
				ConnectionId = connectionId
			};

			switch (eventType)
			{
				case EventType.Ring:
					args.CallerNumber = columns[3];
					args.CalleeNumber = columns[4];
					break;

				case EventType.Connect:
					args.LinePort = int.TryParse(columns[3], out int connectLinePort) ? connectLinePort : null;
					args.CallerNumber = columns[4];
					break;

				case EventType.Disconnect:
					if (int.TryParse(columns[3], out int durationSeconds))
						args.Duration = TimeSpan.FromSeconds(durationSeconds);
					break;

				case EventType.Call:
					args.LinePort = int.TryParse(columns[3], out int callLinePort) ? callLinePort : null;
					args.CalleeNumber = columns[4];
					args.CallerNumber = columns[5];
					break;

				default:
					return null;
			}

			return args;
		}
	}
}
