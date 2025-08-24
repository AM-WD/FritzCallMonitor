using System;
using System.Threading;
using AMWD.Net.Api.Fritz.CallMonitor;

namespace FritzCallMonitor.Demo
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var cts = new CancellationTokenSource();

			Console.CancelKeyPress += (sender, e) =>
			{
				cts.Cancel();
				e.Cancel = true;
			};

			Console.WriteLine("FRITZ!Box Call Monitor Demo");
			Console.WriteLine();

			Console.Write("Enter the FRITZ!Box host (e.g., fritz.box): ");
			string? host = Console.ReadLine()?.Trim();

			if (string.IsNullOrEmpty(host))
			{
				Console.WriteLine("Host is required.");
				return;
			}

			Console.Write("Enter the port (default is 1012): ");
			string? portInput = Console.ReadLine()?.Trim();
			int port = 1012;
			if (!string.IsNullOrEmpty(portInput) && !int.TryParse(portInput, out port))
			{
				Console.WriteLine("Invalid port number. Using default port 1012.");
				port = 1012;
			}

			using (var client = new CallMonitorClient(host, port))
			{
				client.OnEvent += (sender, e) =>
				{
					switch (e.Event)
					{
						case EventType.Ring:
							Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss K} | #{e.ConnectionId} | Incoming Call from {e.CallerNumber} to {e.CalleeNumber}");
							break;

						case EventType.Connect:
							Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss K} | #{e.ConnectionId} | Call connected to {e.CallerNumber}");
							break;

						case EventType.Disconnect:
							Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss K} | #{e.ConnectionId} | Call disconnected after {e.Duration}");
							break;

						case EventType.Call:
							Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss K} | #{e.ConnectionId} | Outgoing Call from {e.CalleeNumber} to {e.CallerNumber}");
							break;
					}
				};

				SpinWait.SpinUntil(() => cts.IsCancellationRequested);
			}

			cts.Dispose();
		}
	}
}
