using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Net.Api.Fritz.CallMonitor.Wrappers
{
	/// <inheritdoc cref="TcpClient"/>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class TcpClientWrapper : IDisposable
	{
		private readonly TcpClient _tcpClient;

		public TcpClientWrapper()
		{
			_tcpClient = new TcpClient();
		}

		/// <inheritdoc cref="TcpClient.Dispose()"/>
		public virtual void Dispose()
			=> _tcpClient.Dispose();

		/// <inheritdoc cref="TcpClient.Client"/>
		public virtual SocketWrapper Client => new(_tcpClient.Client);

		/// <inheritdoc cref="TcpClient.Connected"/>
		public virtual bool Connected => _tcpClient.Connected;

		/// <inheritdoc cref="TcpClient.ConnectAsync(string, int)"/>
		public virtual Task ConnectAsync(string host, int port)
			=> _tcpClient.ConnectAsync(host, port);

		/// <inheritdoc cref="TcpClient.GetStream()"/>
		public virtual NetworkStreamWrapper GetStream()
			=> new(_tcpClient.GetStream());

#if NET6_0_OR_GREATER
		public virtual Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
			=> _tcpClient.ConnectAsync(host, port, cancellationToken).AsTask();
#endif
	}
}
