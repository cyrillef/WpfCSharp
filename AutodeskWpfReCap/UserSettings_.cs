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

// Why 'static readonly' vs 'public const string'
// http://www.stum.de/2009/01/14/const-strings-a-very-convenient-way-to-shoot-yourself-in-the-foot/

namespace Autodesk.ADN.WpfReCap {

	public class UserSettings {

		// Hard coded consumer and secret keys and base URL.
		// In real world Apps, these values need to secured.
		// One approach is to encrypt and/or obfuscate these values
		public static readonly string CONSUMER_KEY ="your_consumer_key" ;
		public static readonly string CONSUMER_SECRET ="your_consumer_secret_key" ;
		public static readonly string OAUTH_HOST ="https://accounts.autodesk.com/" ; // Autodesk production accounts server
		//public static readonly string OAUTH_HOST ="https://accounts-staging.autodesk.com/" ; // Autodesk staging accounts server

		// ReCap: Fill in these macros with the correct information (only the 2 first are important)
		public static readonly string ReCapAPIURL ="http://rc-api-adn.autodesk.com/3.1/API/" ;
		public static readonly string ReCapClientID ="your_ReCap_client_ID" ;
		//public static readonly string ReCapKey ="your ReCap client key" ; // not used anymore

		// Do not edit
		public static readonly string OAUTH_REQUESTTOKEN ="OAuth/RequestToken" ;
		public static readonly string OAUTH_ACCESSTOKEN ="OAuth/AccessToken" ;
		public static readonly string OAUTH_AUTHORIZE ="OAuth/Authorize" ;
		public static readonly string OAUTH_INVALIDATETOKEN ="OAuth/InvalidateToken" ;
		public static readonly string OAUTH_ALLOW =OAUTH_HOST + "OAuth/Allow" ;

	}

}
