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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;

using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;

namespace Autodesk.ADN.WpfReCap {

	public partial class oAuth : Window {
		private static RestClient _restClient =null ;

		public oAuth () {
			InitializeComponent () ;
		}

		public async void startLogin () {
			if ( await RequestToken () ) // Leg 1
                Authorize () ; // Leg 2
            else
                Close () ;
		}

		public static bool isOOB {
			get { return (false) ; } // Return false always in this example
		}

		//- First Leg: The first step of authentication is to request a token
		protected async Task<bool> RequestToken () {
			try {
				if ( _restClient == null )
					_restClient =new RestClient (UserSettings.OAUTH_HOST) ;

				LogInfo ("Initializing and resetting tokens") ;
				Properties.Settings.Default.oauth_token ="" ;
				Properties.Settings.Default.oauth_token_secret ="" ;
				Properties.Settings.Default.oauth_session_handle ="" ;
				Properties.Settings.Default.x_oauth_user_name ="" ;
				Properties.Settings.Default.x_oauth_user_guid ="" ;
				Properties.Settings.Default.Save () ;

				if ( isOOB )
					_restClient.Authenticator =OAuth1Authenticator.ForRequestToken (UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET, "oob") ;
				else
					_restClient.Authenticator =OAuth1Authenticator.ForRequestToken (UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET) ;
				// Build the HTTP request for a Request token and execute it against the OAuth provider
				var request =new RestRequest (UserSettings.OAUTH_REQUESTTOKEN, Method.POST) ;
				Log (UserSettings.OAUTH_REQUESTTOKEN + " request sent", "Request") ;
				//var response =_restClient.Execute (request) ;
				var response =await _restClient.ExecuteTaskAsync (request) ;
				if ( response.StatusCode != HttpStatusCode.OK ) {
					LogError ("Failure! HTTP request did not work! Maybe you are having a connection problem?") ;
					return (false) ;
				}
        
				// The HTTP request succeeded. Get the request token and associated parameters.
				var requestToken =HttpUtility.ParseQueryString (response.Content) ;
				if ( requestToken.AllKeys.Contains ("xoauth_problem") ) {
					LogError ("Failure! " + requestToken ["xoauth_problem"] + ": " + requestToken ["oauth_error_message"]) ;
					return (false) ;
				}
				if ( requestToken.Count < 2 ) {
					LogError ("Failure! Could not get the Request Tokens! Maybe your keys are incorrect?") ;
					return (false) ;
				}
				Properties.Settings.Default.oauth_token =requestToken ["oauth_token"] ;
				Properties.Settings.Default.oauth_token_secret =requestToken ["oauth_token_secret"] ;
				Properties.Settings.Default.Save () ;
				Log (UserSettings.OAUTH_REQUESTTOKEN + " successful", "Response") ;
				return (true) ;
			} catch ( Exception ex ) {
				LogError ("Exception: " + ex.Message) ;
				return (false) ;
			}
		}

		//- Second Leg: The second step is to authorize the user using the Autodesk login server
		protected void Authorize () {
			LogInfo ("Prepating " + UserSettings.OAUTH_AUTHORIZE + " request") ;
			var request =new RestRequest (UserSettings.OAUTH_AUTHORIZE) ;
			request.AddParameter ("oauth_token", Properties.Settings.Default.oauth_token) ;
			if ( isOOB ) {
				// In case of out-of-band authorization, let's show the authorization page which will provide the user with a PIN
				// in the default browser. Then here in our app request the user to type in that PIN.
				Uri authorizeUri =_restClient.BuildUri (request) ;
				Process.Start (authorizeUri.ToString ()) ;
			} else {
				// Otherwise let's load the page in our web viewer so that we can catch the URL that it gets redirected to
				// viewmode could be: full, mobile, iframe, desktop
				request.AddParameter ("viewmode", "desktop") ;
				webView.Source =_restClient.BuildUri (request) ;
			}
		}

		//- When a new URL is being shown in the browser, we can check the URL
		//- This is needed in case of in-band authorization which will redirect us to a given
		//- URL (OAUTH_ALLOW) in case of success
		private async void webView_LoadCompleted (object sender, System.Windows.Navigation.NavigationEventArgs e) {
			// In case of out-of-band login we do not need to check the callback URL
			// Instead we'll need the PIN that the webpage will provide for the user
			if ( isOOB )
				return ;
			// Let's check if we got redirected to the correct page
			if ( isAuthorizeCallBack () ) {
				LogInfo ("User authorized!") ;
				bool bRet =await AccessToken (false, null) ;
				Close () ;
			} else {
				LogError ("Failure! User not authorized!") ;
			}
		}

		//- Third leg: The third step is to authenticate using the request tokens
		//- Once you get the access token and access token secret you need to use those to make your further REST calls
		//- Same in case of refreshing the access tokens or invalidating the current session. To do that we need to pass
		//- in the acccess token and access token secret as the accessToken and tokenSecret parameter of the
		//- [AdskRESTful URLRequestForPath] function
		public static async Task<bool> AccessToken (bool refresh, string PIN) {
			try {
				if ( _restClient == null )
					_restClient =new RestClient (UserSettings.OAUTH_HOST) ;
				var request =new RestRequest (UserSettings.OAUTH_ACCESSTOKEN, Method.POST) ;

				// If we already got access tokens and now just try to refresh
				// them then we need to provide the session handle
				if ( refresh ) {
					LogInfo ("Refreshing Access Tokens") ;
					if ( Properties.Settings.Default.oauth_session_handle.Length == 0 )
						return (false) ;
					if ( isOOB )
						_restClient.Authenticator =OAuth1Authenticator.ForAccessTokenRefresh (
							UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
							Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret,
							PIN, Properties.Settings.Default.oauth_session_handle
						) ;
					else
						_restClient.Authenticator =OAuth1Authenticator.ForAccessTokenRefresh (
							UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
							Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret,
							Properties.Settings.Default.oauth_session_handle
						) ;
				} else {
					LogInfo ("Acquiring new Access Tokens") ;
					if ( Properties.Settings.Default.oauth_token.Length == 0 )
						return (false) ;
					if ( isOOB )
						// Use PIN to request access token for users account for an out of band request.
						_restClient.Authenticator =OAuth1Authenticator.ForAccessToken (
							UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
							Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret,
							PIN
						) ;
					else
						_restClient.Authenticator =OAuth1Authenticator.ForAccessToken (
							UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
							Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret
						) ;
				}

				Properties.Settings.Default.oauth_token ="" ;
				Properties.Settings.Default.oauth_token_secret ="" ;
				Properties.Settings.Default.oauth_session_handle ="" ;
				Properties.Settings.Default.x_oauth_user_name ="" ;
				Properties.Settings.Default.x_oauth_user_guid ="" ;
				Properties.Settings.Default.Save () ;

				Log (UserSettings.OAUTH_ACCESSTOKEN + " request sent", "Request") ;
				//var response =_restClient.Execute (request) ;
				var response =await _restClient.ExecuteTaskAsync (request) ;
				if ( response.StatusCode != HttpStatusCode.OK ) {
					LogError ("Failure! HTTP request did not work! Maybe you are having a connection problem?") ;
					return (false) ;
				}

				// The HTTP request succeeded. Get the request token and associated parameters.
				var accessToken =HttpUtility.ParseQueryString (response.Content) ;
				if ( accessToken.AllKeys.Contains ("xoauth_problem") ) {
					LogError ("Failure! " + accessToken ["xoauth_problem"] + ": " + accessToken ["oauth_error_message"]) ;
					return (false) ;
				}
				if ( accessToken.Count < 3 || accessToken ["oauth_session_handle"] == null ) {
					if ( refresh )
						LogError ("Failure! Could not refresh Access Tokens!") ;
					else
						LogError ("Failure! Could not get Access Tokens!") ;
					return (false);
				}
				if ( refresh )
					Log ("Success! Access Tokens refreshed!", "Response") ;
				else
					Log ("Success! Access Tokens acquired!", "Response") ;

				// This isn't really secure as the tokens will be stored in the application settings
				// but not protected - This code is there to help testing the sample, but you should consider
				// securing the Access Tokens in a better way, or force login each time the application starts.
				Properties.Settings.Default.oauth_token =accessToken ["oauth_token"] ;
				Properties.Settings.Default.oauth_token_secret =accessToken ["oauth_token_secret"] ;
				Properties.Settings.Default.oauth_session_handle =accessToken ["oauth_session_handle"] ;
				Properties.Settings.Default.x_oauth_user_name =accessToken ["x_oauth_user_name"] ;
				Properties.Settings.Default.x_oauth_user_guid =accessToken ["x_oauth_user_guid"] ;

				// The request returns other parameters that we do not use in this example.
				// They are listed here in comment in case you wanted to use them in your application.
				//double TokenExpire =double.Parse (accessToken ["oauth_expires_in"]) ;
				//double SessionExpire =double.Parse (accessToken ["oauth_authorization_expires_in"]) ;

				Properties.Settings.Default.Save () ;

				return (true) ;
			} catch ( Exception ex ) {
				LogError ("Exception: " + ex.Message) ;
				return (false) ;
			}
		}

		//- If we do not want to use the service anymore then
		//- the best thing is to log out, i.e. invalidate the tokens we got
		public static async Task<bool> InvalidateToken () {
			try {
				if ( _restClient == null )
					_restClient =new RestClient (UserSettings.OAUTH_HOST) ;

				LogInfo ("Initializing to release Access Tokens") ;
				var request =new RestRequest (UserSettings.OAUTH_INVALIDATETOKEN, Method.POST) ;
				_restClient.Authenticator =OAuth1Authenticator.ForAccessTokenRefresh (
					UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
					Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret,
					Properties.Settings.Default.oauth_session_handle
				) ;
				Log (UserSettings.OAUTH_INVALIDATETOKEN + " request sent", "Request") ;
				//var response =_restClient.Execute (request) ;
				var response =await _restClient.ExecuteTaskAsync (request) ;
				if ( response.StatusCode != HttpStatusCode.OK ) {
					LogError ("Failure! HTTP request did not work! Maybe you are having a connection problem?") ;
					return (false) ;
				}

				// If Invalidate was successful, we will not get back any data
				if ( response.Content.Length == 0 ) {
					Properties.Settings.Default.oauth_token ="" ;
					Properties.Settings.Default.oauth_token_secret ="" ;
					Properties.Settings.Default.oauth_session_handle ="" ;
					Properties.Settings.Default.x_oauth_user_name ="" ;
					Properties.Settings.Default.x_oauth_user_guid ="" ;
					Properties.Settings.Default.Save () ;
					Log ("Success! Access Tokens released", "Response") ;
				} else {
					var accessToken =HttpUtility.ParseQueryString (response.Content) ;
					if ( accessToken.AllKeys.Contains ("xoauth_problem") )
						LogError ("Failure! " + accessToken ["xoauth_problem"] + ": " + accessToken ["oauth_error_message"]) ;
					else
						LogError ("Failure! Could not log out!") ;
					return (false) ;
				}
				return (true) ;
			} catch ( Exception ex ) {
				LogError ("Exception: " + ex.Message) ;
				return (false) ;
			}
		}

		//- Check if the URL is OAUTH_ALLOW, which means that the user could log in successfully
		private bool isAuthorizeCallBack () {
			string fullUrlString =webView.Source.AbsoluteUri ;
			if ( fullUrlString.Length == 0 )
				return (false) ;
			string [] arr =fullUrlString.Split ('?') ;
			if ( arr == null || arr.Length != 2 )
				return (false) ;
			// If we were redirected to the OAUTH_ALLOW URL then the user could log in successfully
			if ( arr [0] == UserSettings.OAUTH_ALLOW )
				return (true) ;
			// If we got to this page then probably there is an issue
			if ( arr [0] == UserSettings.OAUTH_AUTHORIZE ) {
				// If the page contains the word "oauth_problem" then there is clearly a problem
				//string content =webView stringByEvaluatingJavaScriptFromString:@"document.body.innerHTML"] ;
				//if ( content.IndexOf ("oauth_problem") > -1 )
				//	LogError ("Failure!\nCould not log in!\nTry again!") ;
			}
			return (false) ;
		}

		private void Window_Loaded (object sender, RoutedEventArgs e) {
			startLogin () ;
		}

		private static void LogInfo (string msg) {
			Log (msg, "Info") ;
		}

		private static void LogError (string msg) {
			Log (msg, "Error") ;
		}

		private static void Log (string msg, string category ="Info") {
			Trace.WriteLine ("oAuth: " + msg, category) ;
		}

	}
}
