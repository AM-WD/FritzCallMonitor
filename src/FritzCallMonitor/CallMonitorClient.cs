using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Net.Api.Fritz.CallMonitor.Utils;
using Microsoft.Extensions.Logging;

namespace AMWD.Net.Api.Fritz.CallMonitor
{
	/// <summary>
	/// Represents a client for monitoring call events from a FRITZ!Box.
	/// </summary>
	/// <remarks>
	/// The FRITZ!Box has a built-in realtime call monitoring feature that can be accessed via TCP on port 1012.
	/// </remarks>
	public class CallMonitorClient : IDisposable
	{
		private bool _isDisposed;

		private ILogger? _logger;
		private readonly ReconnectTcpClient _tcpClient;
		private readonly CancellationTokenSource _disposeCts;

		private Task _monitorTask = Task.CompletedTask;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallMonitorClient"/> class.
		/// </summary>
		/// <param name="host">The hostname or IP address of the FRITZ!Box to monitor.</param>
		/// <param name="port">The port to connect to (Default: 1012).</param>
		/// <exception cref="ArgumentNullException">The hostname is not set.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The port is not in valid range of 1 to 65535.</exception>
		public CallMonitorClient(string host, int port = 1012)
		{
			if (string.IsNullOrWhiteSpace(host))
				throw new ArgumentNullException(nameof(host));

			if (port <= ushort.MinValue || ushort.MaxValue < port)
				throw new ArgumentOutOfRangeException(nameof(port));

			_disposeCts = new CancellationTokenSource();
			_tcpClient = new ReconnectTcpClient(host, port) { OnConnected = OnConnected };

			// Start the client in the background
			_tcpClient.StartAsync(_disposeCts.Token).Forget();
		}

		/// <summary>
		/// Occurs when a call monitoring event is raised.
		/// </summary>
		/// <remarks>
		/// The event provides details using the <see cref="CallMonitorEventArgs"/> parameter.
		/// </remarks>
		public event EventHandler<CallMonitorEventArgs>? OnEvent;

		/// <summary>
		/// Gets or sets a logger instance.
		/// </summary>
		public ILogger? Logger
		{
			get => _logger;
			set
			{
				_logger = value;
				_tcpClient.Logger = value;
			}
		}

		/// <summary>
		/// Releases all resources used by the current instance of the <see cref="CallMonitorClient"/>.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			_disposeCts.Cancel();

			try
			{
				_monitorTask.Wait();
			}
			catch
			{ }

			_tcpClient.Dispose();
			_disposeCts.Dispose();

			GC.SuppressFinalize(this);
		}

		private Task OnConnected(ReconnectTcpClient client)
		{
			Logger?.LogTrace($"Client connected");

			_monitorTask = Task.Run(async () =>
			{
				try
				{
					var stream = client.GetStream();
					if (stream == null)
						return;

					string? buffer = null;
					byte[] rawBuffer = new byte[4096];
					while (!_disposeCts.IsCancellationRequested && client.IsConnected)
					{
						try
						{
							int bytesRead = await stream.ReadAsync(rawBuffer, 0, rawBuffer.Length, _disposeCts.Token);
							string data = Encoding.UTF8.GetString(rawBuffer, 0, bytesRead);

							if (buffer != null)
							{
								data = buffer + data;
								buffer = null;
							}

							if (!data.EndsWith("\n"))
							{
								buffer = data;
								continue;
							}

							string[] lines = data.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
							foreach (string line in lines)
							{
								var eventArgs = CallMonitorEventArgs.Parse(line);
								if (eventArgs == null)
									continue;

								Task.Run(() => OnEvent?.Invoke(this, eventArgs), _disposeCts.Token).Forget();
							}
						}
						catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
						{
							// Client was stopped or disposed.
							return;
						}
						catch (Exception ex) when (!_disposeCts.IsCancellationRequested)
						{
							Logger?.LogError(ex, "Error while reading from the call monitor stream.");
							return;
						}
					}
				}
				catch
				{ }
			});

			return Task.CompletedTask;
		}
	}
}
