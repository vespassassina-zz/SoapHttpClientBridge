using System;
using System.Web.Services.Protocols;
using System.IO;
using System.Net;
using System.Globalization;
using System.Xml;
using System.Web.Services.Description;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Text;


namespace System.Web.Services.Protocols
{

	public class SoapHttpClientBridge:SoapHttpClientProtocol
	{
		const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
		const string Soap12EnvelopeNamespace = "http://www.w3.org/2003/05/soap-envelope";
		const string SoapEncodingNamespace = "http://schemas.xmlsoap.org/soap/encoding/";
		const string Soap12EncodingNamespace = "http://www.w3.org/2003/05/soap-encoding";


		#region cached objects

		//we cahce some stuff to avoid calling again and again just to get the same definition of a static method.

		static Type soapExtensionRuntimeConfigArrayType;

		private static Type SoapExtensionRuntimeConfigArrayType {
			get {
				if (soapExtensionRuntimeConfigArrayType == null) {
					soapExtensionRuntimeConfigArrayType = ReflectionHelper.GetTypeFromAssembly("System.Web.Services", "System.Web.Services.Protocols.SoapExtensionRuntimeConfig", true);

				}
				return soapExtensionRuntimeConfigArrayType;
			}
		}

		static Type methodStubInfoType;

		private static Type MethodStubInfoType {
			get {
				if (methodStubInfoType == null) {
					methodStubInfoType = ReflectionHelper.GetTypeFromAssembly("System.Web.Services", "System.Web.Services.Protocols.SoapMethodStubInfo", false);

				}
				return methodStubInfoType;
			}
		}

		static Type faultType;

		private static Type FaultType {
			get {
				if (faultType == null) {
					faultType = ReflectionHelper.GetTypeFromAssembly("System.Web.Services", "System.Web.Services.Protocols.Fault", false);
				}
				return faultType;
			}
		}

		static XmlSerializer faultSerializer;

		private static XmlSerializer FaultSerializer {
			get {
				if (faultSerializer == null) {
					faultSerializer = (XmlSerializer)ReflectionHelper.CreateInstance("System.Web.Services", "System.Web.Services.Protocols.FaultSerializer", new object[]{ });
				}
				return faultSerializer;
			}
		}

		static XmlSerializer fault12Serializer;

		private static XmlSerializer Fault12Serializer {
			get {
				if (fault12Serializer == null) {
					fault12Serializer = (XmlSerializer)ReflectionHelper.CreateInstance("System.Web.Services", "System.Web.Services.Protocols.Fault12Serializer", new object[]{ });
				}
				return fault12Serializer;
			}
		}



		static Type webServiceHelperType;

		private static Type WebServiceHelperType {
			get {
				if (webServiceHelperType == null) {
					webServiceHelperType = ReflectionHelper.GetTypeFromAssembly("System.Web.Services", "System.Web.Services.Protocols.WebServiceHelper", false);
				}
				return webServiceHelperType;
			}
		}

		static Type soapExtensionType;

		private static Type SoapExtensionType {
			get {
				if (soapExtensionType == null) {
					soapExtensionType = typeof(SoapExtension);
				}
				return soapExtensionType;
			}
		}

		#endregion

		/// <summary>
		/// Invokes the webservice
		/// </summary>
		/// <param name="methodName">Method name.</param>
		/// <param name="parameters">Parameters.</param>
		/// <param name="legacyClient">If set to <c>true</c> uses the original SOAP client as implemented in Mono.</param>
		protected new object[] Invoke(string methodName, object[] parameters, bool legacyClient = false)
		{
			if (legacyClient) {
				return base.Invoke(methodName, parameters);
			}

			try {		
				object theTypeInfoObject = ReflectionHelper.GetFieldValue(this, "type_info");
				object soapMethodStubInfo = ReflectionHelper.ExecuteMethod(theTypeInfoObject, "GetMethod", null, methodName);
				SoapClientMessage soapClientMessage = (SoapClientMessage)ReflectionHelper.CreateInstance("System.Web.Services", "System.Web.Services.Protocols.SoapClientMessage", this, soapMethodStubInfo, base.Url, parameters);
				object methodStubInfo = ReflectionHelper.GetFieldValue(soapClientMessage, "MethodStubInfo");
				object headers = ReflectionHelper.GetFieldValue(methodStubInfo, "Headers"); 


				ReflectionHelper.ExecuteMethod(soapClientMessage, "CollectHeaders", null, this, headers, SoapHeaderDirection.In);

				object[] ClassConfiguredExtensions = (object[])ReflectionHelper.GetFieldValue(theTypeInfoObject, "SoapExtensions");
				object[] MethodConfiguredExtensions = (object[])ReflectionHelper.GetFieldValue(soapMethodStubInfo, "SoapExtensions"); 

				Type[] methodTypes = new Type[] {
					SoapExtensionRuntimeConfigArrayType,
					SoapExtensionRuntimeConfigArrayType,
					SoapExtensionRuntimeConfigArrayType
				};

				//extension chain
				SoapExtension[] extensions = (SoapExtension[])ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "CreateExtensionChain", methodTypes, ClassConfiguredExtensions[0], MethodConfiguredExtensions, ClassConfiguredExtensions[1]);

			
				//uri
				object thisUri = ReflectionHelper.GetFieldValue(this, "uri");



				//body
				string requestData = null;
				using (MemoryStream memoryStream = new MemoryStream()) {
					SerializeRequest(memoryStream, soapClientMessage, extensions);
					requestData = Encoding.UTF8.GetString(memoryStream.ToArray());
				}


				//HttpClient sync call
				using (HttpClientResponse clientResponse = AsyncOperatingContext.Run<HttpClientResponse>(() => { 
					HttpClientHelper.Proxy = Proxy;
					HttpClientHelper.UserAgent = UserAgent;
					HttpClientHelper.EnableDecompression = EnableDecompression;
					HttpClientHelper.Timeout = Timeout;
					return HttpClientHelper.GetResponse((Uri)thisUri, requestData, soapClientMessage);
				})) {
					object[] result = DeserializeResponse(clientResponse.DataStream, clientResponse.StatusCode, clientResponse.ContentType, (SoapClientMessage)soapClientMessage, (SoapExtension[])extensions);
					return result;
				}			


			} catch (Exception ex) {
				Console.Write(ex);
				throw ex;
			} 
		}



		#region internal implementation

		void SerializeRequest(Stream s, SoapClientMessage message, SoapExtension[] extensions)
		{
			try {

				if (extensions != null) {
					s = (Stream)ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "ExecuteChainStream", null, extensions, s);
					ReflectionHelper.ExecuteMethod(message, "SetStage", null, SoapMessageStage.BeforeSerialize);
					ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "ExecuteProcessMessage", null, extensions, message, s, true);
				}

				using (XmlTextWriter xtw = new XmlTextWriter(s, new UTF8Encoding(false))) {

					var methodStubInfo = ReflectionHelper.GetFieldValue(message, "MethodStubInfo");
					var parameters = ReflectionHelper.GetFieldValue(message, "Parameters");
					var isSoap12Bool = (bool)ReflectionHelper.GetPropertyValue(message, "IsSoap12");

					WriteSoapMessage(xtw, methodStubInfo, SoapHeaderDirection.In, parameters, message.Headers, isSoap12Bool);

					if (extensions != null) {
						ReflectionHelper.ExecuteMethod(message, "SetStage", SoapMessageStage.AfterSerialize);
						ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "ExecuteProcessMessage", null, extensions, message, s, true);
					}

					xtw.Flush();
					xtw.Close();
				}

			} catch (Exception ex) {
				Console.Write(ex);
				throw ex;
			} 

		}

		object[] DeserializeResponse(Stream responseBodyStream, HttpStatusCode statusCode, string contentType, SoapClientMessage message, SoapExtension[] extensions)
		{
			try {
				var msi = ReflectionHelper.GetFieldValue(message, "MethodStubInfo");

				//web exception
				if (!(statusCode == HttpStatusCode.Accepted || statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.InternalServerError)) {
					string msg = "The request failed with HTTP status {0}: {1}";
					msg = String.Format(msg, (int)statusCode, statusCode);
					throw new WebException(msg, null, WebExceptionStatus.ProtocolError, null);
				}

				//no response to process
				if (message.OneWay && responseBodyStream.Length <= 0 && (statusCode == HttpStatusCode.Accepted || statusCode == HttpStatusCode.OK)) {
					return new object[0];
				}

		
				string ctype;
				Encoding encoding = GetContentEncoding(contentType, out ctype);
				ctype = ctype.ToLower(CultureInfo.InvariantCulture);

				if (ctype != "text/xml") {
					ReflectionHelper.ExecuteStaticMethod(webServiceHelperType, "InvalidOperation", null, String.Format("Not supported Content-Type in the response: '{0}'", ctype), null, encoding);
				}

				message.ContentType = ctype;
				message.ContentEncoding = encoding.WebName;

				if (extensions != null) {				
					responseBodyStream = (Stream)ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "ExecuteChainStream", null, extensions, responseBodyStream);
					ReflectionHelper.ExecuteMethod(message, "SetStage", null, SoapMessageStage.BeforeDeserialize);
					ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "ExecuteProcessMessage", null, extensions, message, responseBodyStream, true);
				}

				// Deserialize the response
				SoapHeaderCollection headers = null;
				object content = null;

				var isSoap12Bool = ReflectionHelper.GetPropertyValue(message, "IsSoap12");
				var methodStubInfo = ReflectionHelper.GetFieldValue(message, "MethodStubInfo");
				var methodStubInfoHeaders = ReflectionHelper.GetFieldValue(methodStubInfo, "Headers");


				using (StreamReader reader = new StreamReader(responseBodyStream, encoding, false)) {		
					using (XmlTextReader xml_reader = new XmlTextReader(reader)) {				

//						Type[] methodTypes = new Type[] {
//							typeof(XmlTextReader),
//							MethodStubInfoType,
//							typeof(SoapHeaderDirection),
//							typeof(bool),
//							typeof(object),
//							typeof(SoapHeaderCollection)
//						};

						ReadSoapMessage(xml_reader, msi, SoapHeaderDirection.Out, (bool)isSoap12Bool, out content, out headers);
						//(XmlTextWriter)ReflectionHelper.ExecuteStaticMethod (webServiceHelperType, "ReadSoapMessage", methodTypes, xml_reader, msi, SoapHeaderDirection.Out, isSoap12Bool, content, headers);
					}
				}		

				if (content.GetType() == FaultType) {
					object fault = content;
					object faultstring = ReflectionHelper.GetFieldValue(fault, "faultstring");
					object faultcode = ReflectionHelper.GetFieldValue(fault, "faultcode");
					object faultactor = ReflectionHelper.GetFieldValue(fault, "faultactor");
					object faultdetail = ReflectionHelper.GetFieldValue(fault, "detail");
					SoapException ex = new SoapException((string)faultstring, (XmlQualifiedName)faultcode, (string)faultactor, (XmlNode)faultdetail);
					ReflectionHelper.ExecuteMethod(message, "SetException", ex);
				} else {
					ReflectionHelper.SetPropertyValue(message, "OutParameters", (object[])content);
				}

				ReflectionHelper.ExecuteMethod(message, "SetHeaders", null, headers);
				ReflectionHelper.ExecuteMethod(message, "UpdateHeaderValues", null, this, methodStubInfoHeaders);


				if (extensions != null) {
					ReflectionHelper.ExecuteMethod(message, "SetStage", SoapMessageStage.AfterDeserialize);
					ReflectionHelper.ExecuteStaticMethod(SoapExtensionType, "ExecuteProcessMessage", null, extensions, message, responseBodyStream, true);
				}


				if (message.Exception == null) {
					var outParameters = ReflectionHelper.GetPropertyValue(message, "OutParameters");
					return (object[])outParameters;
				} else {
			
					Console.WriteLine(message.Exception.Message);
					throw message.Exception;
				}
			} catch (Exception ex) {

			

				Console.WriteLine(ex.Message);
				throw ex;
			} finally {
			
			}
		}

		static Encoding GetContentEncoding(string cts, out string contentType)
		{
			char[] trimChars = { '"', '\'' };
			string encoding;

			if (cts == null)
				cts = "";

			encoding = "utf-8";
			int start = 0;
			int idx = cts.IndexOf(';');
			if (idx == -1)
				contentType = cts;
			else
				contentType = cts.Substring(0, idx);

			contentType = contentType.Trim();
			for (start = idx + 1; idx != -1;) {
				idx = cts.IndexOf(';', start);
				string body;
				if (idx == -1)
					body = cts.Substring(start);
				else {
					body = cts.Substring(start, idx - start);
					start = idx + 1;
				}
				body = body.Trim();
				if (String.CompareOrdinal(body, 0, "charset=", 0, 8) == 0) {
					encoding = body.Substring(8);
					encoding = encoding.TrimStart(trimChars).TrimEnd(trimChars);
				}
			}

			return Encoding.GetEncoding(encoding);
		}

		static void WriteSoapMessage(XmlTextWriter xtw, Object methodStubInfo, SoapHeaderDirection dir, object bodyContent, SoapHeaderCollection headers, bool soap12)
		{
			SoapBindingUse use = (SoapBindingUse)ReflectionHelper.GetFieldValue(methodStubInfo, "Use");
			SoapBindingUse methodUse = dir == SoapHeaderDirection.Fault ? SoapBindingUse.Literal : use;
			XmlSerializer bodySerializer = (XmlSerializer)ReflectionHelper.ExecuteMethod(methodStubInfo, "GetBodySerializer", null, dir, soap12);
			XmlSerializer headerSerializer = (XmlSerializer)ReflectionHelper.ExecuteMethod(methodStubInfo, "GetHeaderSerializer", null, dir);
			object[] headerArray = (object[])ReflectionHelper.ExecuteMethod(methodStubInfo, "GetHeaderValueArray", null, dir, headers);



			string ns = soap12 ? Soap12EnvelopeNamespace : SoapEnvelopeNamespace;
			string encNS = soap12 ? Soap12EncodingNamespace : SoapEncodingNamespace;

			xtw.WriteStartDocument();
			xtw.WriteStartElement("soap", "Envelope", ns);
			xtw.WriteAttributeString("xmlns", "xsi", null, XmlSchema.InstanceNamespace);
			xtw.WriteAttributeString("xmlns", "xsd", null, XmlSchema.Namespace);

			// Serialize headers
			if (headerArray != null) {
				xtw.WriteStartElement("soap", "Header", ns);
				headerSerializer.Serialize(xtw, headerArray);
				xtw.WriteEndElement();
			}

			// Serialize body
			xtw.WriteStartElement("soap", "Body", ns);

			if (methodUse == SoapBindingUse.Encoded) {
				xtw.WriteAttributeString("encodingStyle", ns, encNS);
			}

			bodySerializer.Serialize(xtw, bodyContent);

			xtw.WriteEndElement();
			xtw.WriteEndElement();
			xtw.Flush();
		}

		static void ReadSoapMessage(XmlTextReader xmlReader, object soapMethodStubInfo, SoapHeaderDirection dir, bool soap12, out object body, out SoapHeaderCollection headers)
		{
			XmlSerializer bodySerializer = (XmlSerializer)ReflectionHelper.ExecuteMethod(soapMethodStubInfo, "GetBodySerializer", null, dir, false);
			XmlSerializer headerSerializer = (XmlSerializer)ReflectionHelper.ExecuteMethod(soapMethodStubInfo, "GetHeaderSerializer", null, dir);

			try {
				xmlReader.MoveToContent();
				string ns = xmlReader.NamespaceURI;

				switch (ns) {
					case SoapEnvelopeNamespace:
						break;
					default:
						throw new SoapException(String.Format("SOAP version mismatch. Namespace '{0}' is not supported in this runtime profile.", ns), VersionMismatchFaultCode(soap12));
				}

				xmlReader.ReadStartElement("Envelope", ns);

				headers = (SoapHeaderCollection)ReflectionHelper.ExecuteStaticMethod(WebServiceHelperType, "ReadHeaders", null, xmlReader, headerSerializer, ns);

				xmlReader.MoveToContent();
				xmlReader.ReadStartElement("Body", ns);
				xmlReader.MoveToContent();

				//we have a SOAP error, deserialize...
				if (xmlReader.LocalName == "Fault" && xmlReader.NamespaceURI == ns) {
					if (ns == Soap12EnvelopeNamespace) {
						bodySerializer = Fault12Serializer;
					} else {
						bodySerializer = FaultSerializer;
					}
				}

	

				body = bodySerializer.Deserialize(xmlReader);
				//asd
			} catch (Exception ex) {
	
				Console.WriteLine(ex.Message);
				throw;
			} finally {
			
			}
		}

		static XmlQualifiedName VersionMismatchFaultCode(bool soap12)
		{
			Type soapException = ReflectionHelper.GetTypeFromAssembly("System.Web.Services", "System.Web.Services.Protocols.SoapException", false);
			return (XmlQualifiedName)ReflectionHelper.GetFieldValue(soapException, "VersionMismatchFaultCode");
		}

		#endregion




	}




}




