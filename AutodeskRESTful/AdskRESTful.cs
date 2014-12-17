// (C) Copyright 2014 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.

//- Written by Cyrille Fauvel, Autodesk Developer Network (ADN)
//- http://www.autodesk.com/joinadn
//- January 20th, 2014
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Net.Cache;
using System.Security.Cryptography;

namespace Autodesk.ADN.Toolkit.RESTful {
	
	public interface AdskRESTfulPlugin {
		//protected Dictionary<string, string> _tokens ;

		//public AdskRESTfulPlugin (Dictionary<string, string> tokens) ;
		string sign (string method, Uri url, Dictionary<string, string> parameters) ;
		string authorizationHeader (Dictionary<string, string> oauth) ;
		Dictionary<string, string> parameters { get; }

	}

	public class AdskOauthPlugin : AdskRESTfulPlugin {
		protected Dictionary<string, string> _tokens ;
		
		protected AdskOauthPlugin () {}

		public AdskOauthPlugin (Dictionary<string, string> tokens) {
			_tokens =tokens ;
			// @"oauth_consumer_key" @"oauth_consumer_secret" @"oauth_token" @"oauth_token_secret"
		}

		// https://dev.twitter.com/docs/auth/creating-signature
		//
		// These values need to be encoded into a single string which will be used later on. The process to build the string is very specific:
		//
		// Percent encode every key and value to be signed.
		// Sort the list of parameters alphabetically[1] by encoded key[2].
		// For each key/value pair:
		//    Append the encoded key to the output string
		//    Append the '=' character to the output string
		//    Append the encoded value to the output string
		//  If there are more key/value pairs remaining, append a '&' character to the output string.
		//  [1] Note: The Oauth spec says to sort lexigraphically, which is the default alphabetical sort for many libraries
		//  [2] Note: In case of two parameters with the same encoded key, the Oauth spec says to continue sorting based on value
		//
		// To encode the HTTP method, base URL, and parameter string into a single string:
		//
		// Convert the HTTP method to uppercase
		// Append the '&' character
		// Append Percent encode the URL
		// Append the '&' character
		// Append Percent encode the parameter string
		public string sign (string method, Uri url, Dictionary<string, string> parameters) {
			// @"oauth_consumer_secret" @"oauth_token_secret"
			string signatureSecret = string.Format ("{0}&{1}",
				AdskRESTful.UrlEncode (_tokens ["oauth_consumer_secret"]),
				(_tokens.ContainsKey ("oauth_token_secret") ? AdskRESTful.UrlEncode (_tokens ["oauth_token_secret"]) : "")
			) ;
			// Convert to UTF-8 & RFC3986 (urlencode) & Sort all parameters
			SortedDictionary<string, string> encodedParameters =new SortedDictionary<string, string> () ;
			foreach ( KeyValuePair<string, string> entry in parameters )
				encodedParameters.Add (AdskRESTful.UrlEncode (entry.Key), AdskRESTful.UrlEncode (entry.Value)) ;
			// Build string to be signed
			//Queue<string> orderedParameters =new Queue<string> () ;
			//foreach ( KeyValuePair<string, string> entry in encodedParameters )
			//	orderedParameters.Enqueue (string.Format ("{0}={1}", entry.Key, entry.Value)) ; 
			string [] parametersArray =new string [encodedParameters.Count] ;
			int n =0 ;
			foreach ( KeyValuePair<string, string> entry in encodedParameters )
				parametersArray [n++] =string.Format ("{0}={1}", entry.Key, entry.Value) ;
			string parametersString =string.Join ("&", parametersArray) ;
			string urlString =url.AbsoluteUri ; // =string.Format ("{0}://{1}{2}", [url scheme], [url host], [url path]) ;
			string stringToSign =string.Format ("{0}&{1}&{2}",
				AdskRESTful.UrlEncode (method.ToUpperInvariant ()),
				AdskRESTful.UrlEncode (urlString),
				AdskRESTful.UrlEncode (parametersString)
			) ;
			// Sign it
			using ( HMACSHA1 hashAlgorithm =new HMACSHA1 (Encoding.UTF8.GetBytes (signatureSecret)) ) {
				return (Convert.ToBase64String (hashAlgorithm.ComputeHash (Encoding.UTF8.GetBytes (stringToSign)))) ;
			}
		}

		public string authorizationHeader (Dictionary<string, string> oauth) {
			// Convert to UTF-8 & RFC3986 (urlencode) & Sort all parameters
			SortedDictionary<string, string> encodedParameters =new SortedDictionary<string, string> () ;
			foreach ( KeyValuePair<string, string> entry in oauth )
				encodedParameters.Add (AdskRESTful.UrlEncode (entry.Key), AdskRESTful.UrlEncode (entry.Value)) ;

			string [] oauthArray =new string [encodedParameters.Count] ;
			int n =0 ;
			foreach ( KeyValuePair<string, string> entry in encodedParameters ) {
				if ( entry.Key != "oauth_signature" )
					oauthArray [n++] =string.Format ("{0}=\"{1}\"", entry.Key, entry.Value) ;
			}
			oauthArray [n++] =string.Format ("{0}=\"{1}\"", AdskRESTful.UrlEncode ("oauth_signature"), encodedParameters [AdskRESTful.UrlEncode ("oauth_signature")]) ;
			string oauthHeader =string.Join (", ", oauthArray) ;
			oauthHeader =string.Format ("OAuth {0}", oauthHeader) ;
			return (oauthHeader) ;
		}

		public Dictionary<string, string> parameters {
			get {
				Dictionary<string, string> oauth =new Dictionary<string, string> () ;
				// @"oauth_consumer_key" @"oauth_nonce" @"oauth_signature_method"
				// @"oauth_timestamp" @"oauth_token" @"oauth_version"
				oauth ["oauth_consumer_key"] =_tokens ["oauth_consumer_key"] ;
				oauth ["oauth_nonce"] =AdskRESTful.nonce () ;
				oauth ["oauth_signature_method"] ="HMAC-SHA1" ;
				oauth ["oauth_timestamp"] =AdskRESTful.timestamp () ;
				if ( _tokens.ContainsKey ("oauth_token") )
					oauth ["oauth_token"] =_tokens ["oauth_token"] ;
				oauth ["oauth_version"] ="1.0" ;
				if ( _tokens.ContainsKey ("oauth_session_handle") )
					oauth ["oauth_session_handle"] =_tokens ["oauth_session_handle"] ;
				return (oauth) ;
			}
		}

	}

	// HttpResponseMessage / HttpRequestMessage - http://stackoverflow.com/questions/11717144/twitter-oauth-authorization-issue-with-net-c
	public class AdskRESTfulResponse {
		public HttpWebResponse urlResponse ;
		//public NSData* data;

		//public bool isOk () {
		//	if ( urlResponse.StatusCode != HttpStatusCode.OK )
		//		return (false) ;
		//	string st =this.ToString () ;
		//	return (st.IndexOf ("<error>") == -1 && st.IndexOf ("<Error>") == -1) ;
		//}

		//override public string ToString () {
		//	return ([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding]) ;
		//}
	}

	public class AdskRESTful {
		protected Uri _url ;
		protected Dictionary<string, string> _options =null ;
		protected AdskRESTfulPlugin _credential =null ;
		protected Dictionary<string, string> _parameters =null ;
		protected Dictionary<string, string> _files =null ;

		public AdskRESTful (string baseURL, Dictionary<string, string> options) {
			_url =new Uri (baseURL) ;
			_options =options ;
			AlwaysMultipart =false ;
			AlwaysSignParameters =false ;
		}

		//[DefaultValue (false)]
		public bool AlwaysMultipart { get; set; }

		//[DefaultValue (false)]
		public bool AlwaysSignParameters { get; set; }

		public void addSubscriber (AdskRESTfulPlugin plugin) {
			_credential =plugin ;
		}

		public void clearParameters () {
			_parameters =null ;
		}

		public void addParameters (Dictionary<string, string> ununcodedParameters) {
			if ( _parameters == null )
				_parameters =ununcodedParameters ;
			else
				_parameters =_parameters.Concat (ununcodedParameters.Where (kvp => !_parameters.ContainsKey (kvp.Key))).ToDictionary (x => x.Key, x => x.Value) ;
		}

		public void clearPostFiles () {
			_files =null ;
		}

		public void addPostFiles (Dictionary<string, string> files) {
			if ( _files == null )
				_files =files ;
			else
				_files =_files.Concat (files.Where (kvp => !_files.ContainsKey (kvp.Key))).ToDictionary (x => x.Key, x => x.Value) ;
		}

		public void clearAllParameters () {
			clearParameters () ;
			clearPostFiles () ;
		}

		protected HttpWebRequest buildRequest (string method, string url, Dictionary<string, string> headers) {
			Uri rUrl ;
			if ( url.ToLower ().Contains ("http") )
				rUrl =new Uri (url) ;
			else
				rUrl =new Uri (_url, url) ;
        
			// If default headers are provided, then merge them under any explicitly provided headers for the request
			Dictionary<string, string> reqHeaders =new Dictionary<string, string> () ;
			if ( headers != null )
				reqHeaders.Concat (headers) ;
        
			// Get credential (Oauth) parameters
			// Get query (GET) and/or body (POST) + Credential (Oauth) parameters
			Dictionary<string, string> oauth =_credential.parameters ;
			// Combine oAuth & parameters
			Dictionary<string, string> dict =oauth ;
			
			// http://tools.ietf.org/html/rfc5849#section-3.4.1.3 << Oauth 1.0a

			// Include all GET and POST parameters before generating the signature
			// according to the RFC 5849 - The OAuth 1.0 Protocol http://tools.ietf.org/html/rfc5849#section-3.4.1
			// if this change causes trouble we need to introduce a flag indicating the specific OAuth implementation level,
			// or implement a seperate class for each OAuth version
			if ( AlwaysSignParameters || ((_files == null || _files.Count == 0) && !AlwaysMultipart) ) {
				dict =dict.Concat (_parameters)
					.GroupBy (kvp => kvp.Key, kvp => kvp.Value)
					.ToDictionary (g => g.Key, g => g.Last ()) ;
			}
			if ( dict.ContainsKey ("realm") == true )
				dict.Remove ("realm") ;

			oauth ["oauth_signature"] =_credential.sign (method, rUrl, dict) ;
			reqHeaders ["Authorization"] =_credential.authorizationHeader (oauth) ;
			System.Diagnostics.Debug.WriteLine ("Authorization: " + reqHeaders ["Authorization"]) ;
			//reqHeaders ["User-Agent"] ="AdskRESTful/1.0" ;
			//reqHeaders ["Accept-Encoding"] =null/*@"gzip, deflate"*/ ;
			//reqHeaders ["Accept-Language"] =null ;
			//reqHeaders ["Accept"] =null ;
			//reqHeaders ["Pragma"] =null ;
        
			if ( method.ToLower () == "get" ) {
				// Convert to UTF-8 & RFC3986 (urlencode)
				string [] encodedParameters =new string [_parameters.Count] ;
				int n =0 ;
				foreach ( KeyValuePair<string, string> entry in _parameters )
					encodedParameters [n++] =string.Format ("{0}={1}", AdskRESTful.UrlEncode (entry.Key), AdskRESTful.UrlEncode (entry.Value)) ;
				string queryString =string.Join ("&", encodedParameters) ;
				string st =string.Format ("{0}{1}{2}",
					rUrl.AbsoluteUri,
					rUrl.AbsoluteUri.Contains ("?") ? "&" : "?",
					queryString
				) ;
				rUrl =new Uri (st) ;
			}
			HttpWebRequest req =(HttpWebRequest)WebRequest.Create (rUrl) ;
			req.KeepAlive =false ;
			req.Method =method.ToUpperInvariant () ;
			HttpRequestCachePolicy policy =new HttpRequestCachePolicy (HttpRequestCacheLevel.NoCacheNoStore) ;
			req.CachePolicy =policy ;
			//req.Timeout =100 ;
			req.UserAgent ="AdskRESTful/1.0" ;
			foreach ( KeyValuePair<string, string> entry in reqHeaders )
				req.Headers.Add (entry.Key, entry.Value);

			if (   method.ToLower () == "post"
				|| method.ToLower () == "put"
				|| method.ToLower () == "delete"
			) {
				string bodyString ="" ;
				if ( !AlwaysMultipart && (_files == null || _files.Count == 0) && _parameters.Count != 0 ) {
					// Convert to UTF-8 & RFC3986 (urlencode)
					if ( _parameters != null ) {
						string [] encodedParameters =new string [_parameters.Count] ;
						int n =0 ;
						foreach ( KeyValuePair<string, string> entry in _parameters )
							encodedParameters [n++] =string.Format ("{0}={1}", AdskRESTful.UrlEncode (entry.Key), AdskRESTful.UrlEncode (entry.Value)) ;
						bodyString =string.Join ("&", encodedParameters) ;
					}
					req.ContentType ="application/x-www-form-urlencoded; charset=utf-8" ;
				} else {
					string boundaryCode =AdskRESTful.nonce ().Substring (0, 12).ToLower () ;
					string boundary ="----------------------------" + boundaryCode ;
					req.ContentType ="multipart/form-data; boundary=" + boundary ;
					if ( _parameters != null ) {
						foreach ( KeyValuePair<string, string> entry in _parameters ) {
							bodyString +="--" + boundary + "\r\n" ;
							bodyString +="Content-Disposition: form-data; name=\"" + entry.Key + "\"\r\n\r\n" ;
							bodyString +=entry.Value + "\r\n" ;
						}
					}
					if ( _files != null ) {
						int i =0 ;
						foreach ( KeyValuePair<string, string> entry in _files ) {
							string keyName =string.Format ("file[{0}]", i) ;
							//string contentType =[AdskRESTful fileMIMEType:key] ;
							string contentType ="image/jpeg" ;
							bodyString +="--" + boundary + "\r\n" ;
							bodyString +="Content-Disposition: form-data; name=\"" + keyName + "\"; filename=\"" + AdskRESTful.UrlEncode (entry.Key) + "\"\r\n\r\n" ;
							bodyString +="Content-Type: " + contentType + "\r\n\r\n" ;
							bodyString +=entry.Value ;
							bodyString +="\r\n" ;
							i++ ;
						}
					}
					bodyString +="--" + boundary + "--\r\n" ;
				}
				byte [] byte1 =Encoding.UTF8.GetBytes (bodyString) ;
				//req.ContentLength =byte1.Length ;
				try {
					System.IO.Stream body =req.GetRequestStream () ;
					body.Write (byte1, 0, byte1.Length) ;
					body.Close () ;
				} catch ( Exception ex ) {
					System.Diagnostics.Debug.WriteLine (ex.Message) ;
				}
			}
			return (req) ;
		}

		public HttpWebRequest get (string url, Dictionary<string, string> headers) {
			return (buildRequest ("GET", url, headers)) ;
		}

		public HttpWebRequest post (string url, Dictionary<string, string> headers) {
			return (buildRequest ("POST", url, headers));
		}

		public HttpWebRequest put (string url, Dictionary<string, string> headers) {
			return (buildRequest ("PUT", url, headers));
		}

		public HttpWebRequest delete (string url, Dictionary<string, string> headers) {
			return (buildRequest ("DELETE", url, headers));
		}

		public AdskRESTfulResponse send (HttpWebRequest request) {
			AdskRESTfulResponse response =new AdskRESTfulResponse () ;
			try {
				response.urlResponse =(HttpWebResponse)request.GetResponse () ;
			} catch ( Exception ex ) {
				System.Diagnostics.Debug.WriteLine (ex.Message) ;
			}
			return (response) ;
		}

		private static readonly Random _random =new Random () ;
		private static readonly object _randomLock =new object () ;
		public static string nonce () {
			//return (Convert.ToBase64String (Encoding.UTF8.GetBytes (DateTime.UtcNow.Ticks.ToString ()))) ;
			const string Digit ="1234567890" ;
			const string Lower ="abcdefghijklmnopqrstuvwxyz" ;
			const string chars =(Lower + Digit) ;
			var nonce =new char [16] ;
			lock ( _randomLock ) {
				for ( var i =0 ; i < nonce.Length ; i++ )
					nonce [i] =chars [_random.Next (0, chars.Length)] ;
			}
			return (new string (nonce)) ;
		}

		public static string timestamp () {
			//return (DateTime.UtcNow.Ticks.ToString ()) ;
			var timeSpan =(DateTime.UtcNow - new DateTime (1970, 1, 1)) ;
			var timestamp =(long)timeSpan.TotalSeconds ;
			return (timestamp.ToString ()) ;
		}

		public static string HMAC_SHA1 (string stringToSign, string signatureSecret) {
			//using ( HMACSHA1 hashAlgorithm =new HMACSHA1 (Encoding.UTF8.GetBytes (signatureSecret)) ) {
			//	return (Convert.ToBase64String (hashAlgorithm.ComputeHash (Encoding.UTF8.GetBytes (stringToSign)))) ;
			//}
			HMACSHA1 hashAlgorithm =new HMACSHA1 () ;
			hashAlgorithm.Key =Encoding.UTF8.GetBytes (signatureSecret) ;
			return (Convert.ToBase64String (hashAlgorithm.ComputeHash (Encoding.UTF8.GetBytes (stringToSign)))) ;
		}

		//+ (NSString *)fileMIMEType:(NSString*)filename {
			//CFStringRef UTI =UTTypeCreatePreferredIdentifierForTag (kUTTagClassFilenameExtension, (CFStringRef)[filename pathExtension], NULL) ;
			//CFStringRef MIMEType =UTTypeCopyPreferredTagWithClass (UTI, kUTTagClassMIMEType) ;
			//CFRelease (UTI) ;
			//return ([(NSString *)MIMEType autorelease]) ;
		//}

		public static Dictionary<string, string> ParseQueryString (string queryString) {
			Dictionary<string, string> parameters =new Dictionary<string, string> () ;
			string [] split =queryString.Split ('&') ;
			foreach ( string str in split ) {
				string [] split2 =str.Split ('=') ;
				if ( split2.Length > 1 )
					parameters.Add (Uri.UnescapeDataString (split2 [0]), Uri.UnescapeDataString (split2 [1])) ;
			}
			return (parameters) ;
		}

		// URL encodes a string based on section 5.1 of the OAuth spec.
		// Namely, percent encoding with [RFC3986], avoiding unreserved characters,
		// upper-casing hexadecimal characters, and UTF-8 encoding for text value pairs.
		
		// The <see cref="Uri.EscapeDataString"/> method is <i>supposed</i> to take on
		// RFC 3986 behavior if certain elements are present in a .config file.
		// We can't rely on every host actually having this configuration element present.
		
		// see also http://oauth.net/core/1.0#encoding_parameters"
		// see also http://stackoverflow.com/questions/846487/how-to-get-uri-escapedatastring-to-comply-with-rfc-3986"

		// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
		// see also http://stackoverflow.com/questions/846487/how-to-get-uri-escapedatastring-to-comply-with-rfc-3986"

		private static readonly string [] UriRfc3986CharsToEscape =new [] { "!", "*", "'", "(", ")" } ;
		private static readonly string [] UriRfc3968EscapedHex =new [] { "%21", "%2A", "%27", "%28", "%29" } ;

		public static string UrlEncode (string value) {
			// Start with RFC 2396 escaping by calling the .NET method to do the work.
			// This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
			// If it does, the escaping we do that follows it will be a no-op since the
			// characters we search for to replace can't possibly exist in the string.
			StringBuilder escaped =new StringBuilder (Uri.EscapeDataString (value)) ;
			// Upgrade the escaping to RFC 3986, if necessary.
			for ( int i =0 ; i < UriRfc3986CharsToEscape.Length ; i++ ) {
				string t =UriRfc3986CharsToEscape [i] ;
				escaped.Replace (t, UriRfc3968EscapedHex [i]) ;
			}
			// Return the fully-RFC3986-escaped string.
			return (escaped.ToString ()) ;
		}

	}

}
