using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Net.Api.Fritz.CallMonitor.Wrappers;
using Microsoft.Extensions.Logging;

namespace AMWD.Net.Api.Fritz.CallMonitor.Utils
{
	internal class ReconnectTcpClient : IDisposable
	{
		private bool _isDisposed;
		private readonly SemaphoreSlim _connectLock = new(1, 1);

		private readonly string _host;
		private readonly int _port;

		private TcpClientWrapper? _tcpClient;
		private readonly TcpClientWrapperFactory _tcpClientFactory = new();

		private CancellationTokenSource? _stopCts;
		private Task _monitorTask = Task.CompletedTask;

		public ReconnectTcpClient(string host, int port)
		{
			if (string.IsNullOrWhiteSpace(host))
				throw new ArgumentNullException(nameof(host), "The host is required.");

			if (port <= ushort.MinValue || ushort.MaxValue < port)
				throw new ArgumentOutOfRangeException(nameof(port), $"The port must be between {ushort.MinValue + 1} and {ushort.MaxValue}.");

			_host = host;
			_port = port;
		}

		public virtual bool IsConnected => _tcpClient?.Connected ?? false;

		public virtual ILogger? Logger { get; set; }

		public virtual Func<ReconnectTcpClient, Task>? OnConnected { get; set; }

		public virtual void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			StopAsyncInternally(CancellationToken.None).Wait();

			_connectLock.Dispose();
		}

		public virtual NetworkStreamWrapper? GetStream()
		{
			ThrowIfDisposed();
			return _tcpClient?.GetStream();
		}

		public virtual async Task StartAsync(CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			_stopCts = new CancellationTokenSource();

			using (var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopCts.Token))
			{
				await ConnectWithRetryAsync(combinedTokenSource.Token).ConfigureAwait(false);
				if (combinedTokenSource.IsCancellationRequested)
					return;
			}

			_monitorTask = Task.Run(() => MonitorConnectionAsync(_stopCts.Token), _stopCts.Token);
		}

		public virtual Task StopAsync(CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();
			return StopAsyncInternally(cancellationToken);
		}

		private async Task StopAsyncInternally(CancellationToken cancellationToken)
		{
			var stopTask = Task.Run(async () =>
			{
				_stopCts?.Cancel();
				try
				{
					await _monitorTask.ConfigureAwait(false);
				}
				catch
				{ }

				_monitorTask = Task.CompletedTask;

				_stopCts?.Dispose();
				_stopCts = null;

				_tcpClient?.Dispose();
				_tcpClient = null;
			});

			try
			{
				await Task.WhenAny(stopTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
			}
			catch
			{ }
		}

		private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
		{
			await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (_isDisposed || IsConnected)
					return;

				int delay = 250;
				while (!cancellationToken.IsCancellationRequested && !_isDisposed)
				{
					try
					{
						_tcpClient?.Dispose();

						_tcpClient = _tcpClientFactory.Create();
						_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

#if NET6_0_OR_GREATER
						var connectTask = _tcpClient.ConnectAsync(_host, _port, cancellationToken);
#else
						var connectTask = _tcpClient.ConnectAsync(_host, _port);
#endif
						var completedTask = await Task.WhenAny(connectTask, Task.Delay(1000, cancellationToken)).ConfigureAwait(false);
						if (completedTask != connectTask)
							throw new TimeoutException("Connection attempt timed out.");

						if (OnConnected != null)
							await OnConnected(this).ConfigureAwait(false);

						return;
					}
					catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
					{
						// Client was stopped or disposed.
						return;
					}
					catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
					{
						delay *= 2;

						// Limit the delay to a maximum of 1 minute.
						if (delay > 60 * 1000)
							delay = 60 * 1000;

						Logger?.LogWarning(ex, $"Failed to connect to {_host}:{_port}. Retrying in {delay}ms...");
						try
						{
							await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
						}
						catch
						{ }
					}
				}
			}
			finally
			{
				_connectLock.Release();
			}
		}

		private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
		{
			try
			{
				byte[] buffer = new byte[1];
				while (!cancellationToken.IsCancellationRequested && !_isDisposed)
				{
					if (!IsConnected)
					{
						await ConnectWithRetryAsync(cancellationToken).ConfigureAwait(false);
						continue;
					}

					var stream = _tcpClient?.GetStream();
					if (stream != null && stream.CanRead)
					{
						bool disconnected = false;
						try
						{
							// Attempt to read zero bytes to check if the connection is still alive.
							// Should return immediately if the connection is still active.
							// So the timeout of 1sec is more than enough.
							var readTask = stream.ReadAsync(buffer, 0, 0, cancellationToken);
							var completedTask = await Task.WhenAny(readTask, Task.Delay(1000, cancellationToken)).ConfigureAwait(false);
							if (completedTask != readTask)
								continue; // Timeout
						}
						catch
						{
							disconnected = true;
						}

						if (disconnected || !IsConnected)
							await ConnectWithRetryAsync(cancellationToken).ConfigureAwait(false);
					}

					// Check for an active connection every 5 seconds.
					await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{ }
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
		}
	}
}
