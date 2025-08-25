# FRITZ!Box CallMonitor

The call monitor is a custom endpoint, which can be enabled in the FRITZ!Box using specific phone codes.

When the endpoint is enabled, a raw TCP stream is opened on port 1012 of the FRITZ!Box, which sends information about incoming and outgoing calls in real-time.

This can be used to integrate the FRITZ!Box call signaling into other applications or systems.

## Enabling the Call Monitor

To enable the call monitor, you need to dial the following code on a connected telephone: `#96*5*`.

To disable the call monitor, dial: `#96*4*`.



---

Published under [MIT License] (see [choose a license]).

[![Buy me a Coffee](https://shields.io/badge/PayPal-Buy_me_a_Coffee-yellow?style=flat&logo=paypal)](https://link.am-wd.de/donate)


[MIT License]: LICENSE.txt
[choose a license]: https://choosealicense.com/licenses/mit/
