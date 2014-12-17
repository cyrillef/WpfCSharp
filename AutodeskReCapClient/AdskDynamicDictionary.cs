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
	
	public class AdskDynamicDictionary : DynamicObject {
		public Dictionary<string, object> Dictionary { get; internal set; }

		public AdskDynamicDictionary () : base () {
			Dictionary =new Dictionary<string, object> () ;
		}

		public int Count {
			get { return (Dictionary.Count) ; }
		}

		public override bool TryGetMember (GetMemberBinder binder, out object result) {
			// Converting the property name to lowercase so that property names become case-insensitive. 
			string name =binder.Name/*.ToLower ()*/ ;
			// If the property name is found in a dictionary, set the result parameter to the property value and return true. 
			// Otherwise, return false. 
			return (Dictionary.TryGetValue (name, out result)) ;
		}

		// If you try to set a value of a property that is not defined in the class, this method is called. 
		public override bool TrySetMember (SetMemberBinder binder, object value) {
			// Converting the property name to lowercase so that property names become case-insensitive.
			Dictionary [binder.Name/*.ToLower ()*/] =value ;
			// You can always add a value to a dictionary, so this method always returns true. 
			return (true) ;
		}

	}

}
