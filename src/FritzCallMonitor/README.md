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
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Incoming Call from {e.CallerNumber} to {e.CalleeNumber}");
				break;

			case EventType.Connect:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Call connected to {e.CallerNumber}");
				break;

			case EventType.Disconnect:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Call disconnected after {e.Duration}");
				break;

			case EventType.Call:
				Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | #{e.ConnectionId} | Outgoing Call from {e.CalleeNumber} to {e.CallerNumber}");
				break;
		}
	};

	// Wait to terminate.
}
```



---

Published under MIT License (see [choose a license]).

[![Buy me a Coffee](https://shields.io/badge/PayPal-Buy_me_a_Coffee-yellow?style=flat&logo=paypal)](https://link.am-wd.de/donate)

[choose a license]: https://choosealicense.com/licenses/mit/
