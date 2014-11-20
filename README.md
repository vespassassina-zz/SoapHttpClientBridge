SoapHttpClient fix for SoapHttpClientProtocol on MONO
=====================================================

Greetings Xamarin developer,
this little project is meant to fix the TLS issue on mono while using webservices whose proxies inherit from SoapHttpClientProtocol.

A brief history:
it looks like mono does not implement TLS 1.2 or anything better that TLS 1.0 (with some limitations).
to overcome this problem you could switch your whole architecture to use the new HttpClient libs in conjunction with ModernHttpClient component, in this way all the requests would be routed trough the native http client library implementations (ios and android).
Since this was not an option for us (rebuilding our whole SOAP/WCF api was not easily feasible) we had to find a different solution, subclassing SoapHttpClientProtocol to use the new HttpClient and ModernHttpClient, hacking into one of the basic blocks of the soap protocol.

Installation:
* link the projects into your app
* add ModernHttpClient (version 2.1.2+) to your apps and to the SoapHttpClient project
* let your webservice reference inherit from SoapHttpClientBridge instead of SoapHttpClientProtocol
* enjoy

Known issues:
* since we didn't need to we just implemented the Invoke method, leaving all the async or delegate implementations as they were
* in debug mode, on ios real devices (no simulator), heavy usage can lead to deadlocks 
* the library relies heavily on reflection, thus performances could suffer a bit

Stuff to do:
* implement InvokeAsync to use the bridge on async await style operations
* implement BeginInvoke EndInvoke to use the bridge on old async style operations
