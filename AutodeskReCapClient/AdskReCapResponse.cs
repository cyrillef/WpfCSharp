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
//- April 20th, 2014
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Dynamic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Autodesk.ADN.Toolkit.ReCap {

	public class AdskReCapResponse : AdskDynamicDictionary {

		public AdskReCapResponse (XDocument doc) : base () {
			ProcessElement (doc.Element ("Response"), this) ;
		}

		public AdskReCapResponse (JObject obj) : base () {
			ProcessObject (obj, this) ;
		}

		public AdskReCapResponse (JArray obj) : base () {
			ProcessArray (obj, this) ;
		}

		#region Reading object utilities
		internal static void ProcessElement (XElement obj, AdskDynamicDictionary dict) {
			var elList =from el in obj.Elements () select el ;
			foreach ( XElement elt in elList ) {
				if ( elt.HasElements == false ) {
					dict.Dictionary [elt.Name.LocalName] =(string)elt ;
				} else {
					// Careful, might be an array
					bool isArray =(elt.Elements ().Count () != elt.Elements().Select (el => el.Name).Distinct ().Count ()) ;
					AdskDynamicDictionary subDict =new AdskDynamicDictionary () ;
					if ( isArray )
						ProcessElementArray (elt, subDict) ;
					else
						ProcessElement (elt, subDict) ;
					dict.Dictionary [elt.Name.LocalName] =subDict ;
				}
			}
		}

		internal static void ProcessElementArray (XElement obj, AdskDynamicDictionary dict) {
			var elList = from el in obj.Elements () select el ;
			int i =0 ;
			foreach ( XElement elt in elList ) {
				if ( elt.HasElements == false ) {
					dict.Dictionary [i.ToString ()] =(string)elt ;
				} else {
					// Careful, might be an array
					bool isArray =(elt.Elements ().Count () != elt.Elements().Select (el => el.Name).Distinct ().Count ()) ;
					AdskDynamicDictionary subDict =new AdskDynamicDictionary () ;
					ProcessElement (elt, subDict) ;
					if ( isArray )
						ProcessElementArray (elt, subDict) ;
					else
						ProcessElement (elt, subDict) ;
					dict.Dictionary [i.ToString ()] = subDict ;
				}
				i++ ;
			}
		}

		internal static void ProcessObject (JObject obj, AdskDynamicDictionary dict) {
			foreach ( KeyValuePair<string, JToken> pair in obj ) {
				if ( pair.Value.GetType () == typeof (JValue) ) {
					dict.Dictionary [pair.Key] =((JValue)pair.Value).Value ;
				} else if ( pair.Value.GetType () == typeof (JObject) ) {
					AdskDynamicDictionary subDict =new AdskDynamicDictionary () ;
					ProcessObject ((JObject)(pair.Value), subDict) ;
					dict.Dictionary [pair.Key] =subDict ;
				} else if ( pair.Value.GetType () == typeof (JArray) ) {
					AdskDynamicDictionary subDict =new AdskDynamicDictionary () ;
					ProcessArray ((JArray)(pair.Value), subDict) ;
					dict.Dictionary [pair.Key] =subDict ;
				}
			}
		}

		internal static void ProcessArray (JArray obj, AdskDynamicDictionary dict) {
			int i =0 ;
			foreach ( JToken item in obj ) {
				if ( item.GetType () == typeof (JValue) ) {
					dict.Dictionary [i.ToString ()] =((JValue)item).Value ;
				} else if ( item.GetType () == typeof (JObject) ) {
					AdskDynamicDictionary subDict =new AdskDynamicDictionary ();
					ProcessObject ((JObject)(item), subDict) ;
					dict.Dictionary [i.ToString ()] =subDict ;
				} else if ( item.GetType () == typeof (JArray) ) {
					AdskDynamicDictionary subDict =new AdskDynamicDictionary ();
					ProcessArray ((JArray)item, subDict) ;
					dict.Dictionary [i.ToString ()] =subDict ;
				}
				i++ ;
			}
		}

		#endregion

	}

}
