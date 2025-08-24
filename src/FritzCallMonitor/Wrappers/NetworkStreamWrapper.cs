using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Net.Api.Fritz.CallMonitor.Wrappers
{
	/// <inheritdoc cref="NetworkStream"/>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class NetworkStreamWrapper : IDisposable
	{
		private readonly NetworkStream _networkStream;

		public NetworkStreamWrapper(NetworkStream networkStream)
		{
			_networkStream = networkStream;
		}

		/// <inheritdoc cref="Stream.Dispose()"/>
		public virtual void Dispose()
			=> _networkStream.Dispose();

		/// <inheritdoc cref="NetworkStream.CanRead"/>
		public virtual bool CanRead =>
			_networkStream.CanRead;

		/// <inheritdoc cref="Stream.ReadAsync(byte[], int, int, CancellationToken)"/>
		public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _networkStream.ReadAsync(buffer, offset, count, cancellationToken);
	}
}
