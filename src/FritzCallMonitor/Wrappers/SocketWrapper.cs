using System;
using System.Net.Sockets;

namespace AMWD.Net.Api.Fritz.CallMonitor.Wrappers
{
	/// <inheritdoc cref="Socket"/>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class SocketWrapper : IDisposable
	{
		private readonly Socket _socket;

		public SocketWrapper(Socket socket)
		{
			_socket = socket;
		}

		/// <inheritdoc cref="Socket.Dispose()"/>
		public virtual void Dispose()
			=> _socket.Dispose();

		/// <inheritdoc cref="Socket.SetSocketOption(SocketOptionLevel, SocketOptionName, bool)"/>
		public virtual void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
			=> _socket.SetSocketOption(optionLevel, optionName, optionValue);
	}
}
