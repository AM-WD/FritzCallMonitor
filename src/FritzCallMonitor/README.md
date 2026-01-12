# FRITZ!Box Call Monitor

You can use this small library to connect to the FRITZ!Box Call Monitor and receive live call events.


## Router preparation

Call `#96*5*` to enable the endpoint in your FRITZ!Box.    
Call `#96*4*` to disable it again.


## In your code

```csharp
using (var client = new CallMonitorClient(host, port))
{
	client.OnEvent += (sender, e) =>
	{
		switch (e.Event)
		{
			case EventType.Ring:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Incoming Call from {e.ExternalNumber} to {e.InternalNumber}");
				break;

			case EventType.Connect:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Call connected to {e.ExternalNumber}");
				break;

			case EventType.Disconnect:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Call disconnected after {e.Duration}");
				break;

			case EventType.Call:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Outgoing Call from {e.InternalNumber} to {e.ExternalNumber}");
				break;
		}
	};

	// Wait to terminate.
}
```



---

Published under MIT License (see [choose a license]).


[choose a license]: https://choosealicense.com/licenses/mit/
