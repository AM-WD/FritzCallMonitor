using System.Runtime.Serialization;

namespace AMWD.Net.Api.Fritz.CallMonitor
{
	/// <summary>
	/// Represents the types of events that can occur during a call lifecycle on a FRITZ!Box.
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// A call is incoming to the Fritz!Box.
		/// </summary>
		[EnumMember(Value = "RING")]
		Ring = 1,

		/// <summary>
		/// A call is connected - the parties are now talking.
		/// </summary>
		[EnumMember(Value = "CONNECT")]
		Connect = 2,

		/// <summary>
		/// A call is disconnected - one party has hung up.
		/// </summary>
		[EnumMember(Value = "DISCONNECT")]
		Disconnect = 3,

		/// <summary>
		/// A call is outgoing from the Fritz!Box.
		/// </summary>
		[EnumMember(Value = "CALL")]
		Call = 4,
	}
}
