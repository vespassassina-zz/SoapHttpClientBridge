using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ModernHttpClient;

namespace System.Web.Services.Protocols
{
	/// <summary>
	/// Little wrapper class for the result returned by the calls
	/// </summary>
	public class HttpClientResponse : IDisposable
	{
		public HttpStatusCode StatusCode;
		public string ContentType;
		public Stream DataStream;


		public void Dispose()
		{
			if (DataStream != null) {
				DataStream.Dispose();
				DataStream = null;
			}
		}
	}


	/// <summary>
	/// This is the little pulsating heart of our implementation. 
	/// Actually it takes a soap message and sends it to the correct adress.
	/// </summary>
	public static class HttpClientHelper
	{
		public static string UserAgent = "Mono Soap HttpClient Fix for WCF 1.0";
		public static bool EnableDecompression = true;
		public static IWebProxy Proxy = null;
		public static int Timeout = 30;


		static CookieContainer globalCookieContainer;

		static CookieContainer GlobalCookieContainer {
			get {
				if (globalCookieContainer == null) {
					globalCookieContainer = new CookieContainer();
				}
				return globalCookieContainer;
			}
		}

		private static HttpClient GetHttpClientForAddress(string address)
		{		
			// ok, this little boolean is used to enable the ServiceEndpoint manual certificate validation
			// BUT in android it doesn't work, since ModernHttpClient DOES not implement the thing correctly. 
			// anyway it would work for ios.
			const bool ManualCertificateValidation = false;

			var handler = new NativeMessageHandler(false, ManualCertificateValidation) {
				CookieContainer = GlobalCookieContainer,
				UseCookies = true
			};
				
			//proxy, seriously?
			if (Proxy != null && handler.SupportsProxy) {
				handler.Proxy = Proxy;
			}

			if (EnableDecompression && handler.SupportsAutomaticDecompression) {
				handler.AutomaticDecompression = DecompressionMethods.GZip;
			}							

			var client = new HttpClient(handler) {
				BaseAddress = new Uri(address),
				Timeout = new TimeSpan(0, 0, Timeout)
			};

			return client;
		}

		public static async Task<HttpClientResponse> GetResponse(Uri address, string requestBody, SoapClientMessage message)
		{

			//Create the HttpClient for this server
			using (HttpClient client = GetHttpClientForAddress(address.Scheme + "://" + address.Host)) {

				//Headers
				bool isSoap12 = (bool)ReflectionHelper.GetPropertyValue(message, "IsSoap12");
				if (!isSoap12) {
					client.DefaultRequestHeaders.Add("SOAPAction", "\"" + message.Action + "\"");
				}

				//if we use Add instead of TryAddWithoutValidation the client will automatically add commas wherever it finds a space.
				//very ugly and probably not what we want.
				client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

				//Should be: 
				//User-Agent: Mono Custom HttpSoapClient 1.0
				//SOAPAction: "http://tempuri.org/Service/Method"
				//Content-Type: text/xml; charset=utf-8

				//content, with type and encoding, as string
				var content = new StringContent(requestBody, Encoding.UTF8, message.ContentType);

				//post
				using (var result = await client.PostAsync(address, content)) {

					//response wrapper, to avoid passing params by ref
					HttpClientResponse response = new HttpClientResponse();
					response.DataStream = await result.Content.ReadAsStreamAsync();
					response.StatusCode = result.StatusCode;

					//content type, from result content
					IEnumerable<string> values;
					if (result.Content.Headers.TryGetValues("Content-Type", out values)) {
						response.ContentType = values.FirstOrDefault();
					}
					
					return response;
				}
			}
		}

	}
}

