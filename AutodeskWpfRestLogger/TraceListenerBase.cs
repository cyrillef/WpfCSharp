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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Diagnostics;

namespace Autodesk.ADN.Toolkit.Wpf.RestLogger {

	public class TracePart : IEquatable<TracePart> {
		public DateTime Timestamp { get; set; }
		public int IndentLevel { get; set; }
		public string Category { get; set; }
		public string Info { get; set; }

		public override string ToString () {
			return ("[" + Timestamp.ToString ("T") + "] " + Category + (Category != "" ? ": " : "") + Info) ;
		}

		public virtual string ToString (string format) {
			if ( format == "T" )
				return ("[" + Timestamp.ToString ("T") + "] " + Category + (Category != "" ? ": " : "") + Info) ;
			else
				return (Category + (Category != "" ? ": " : "") + Info) ;
		}

		public override int GetHashCode () {
			return (this.Timestamp.GetHashCode ()) ;
		}

		public override bool Equals (object obj) {
			if ( obj == null )
				return (false) ;
			TracePart objAsPart =obj as TracePart ;
			return (Equals (objAsPart)) ;
		}

		public bool Equals (TracePart other) {
			if ( other == null )
				return (false) ;
			return (this.Category.Equals (other.Category) && this.Info.Equals (other.Info)) ;
		}

		public static bool operator == (TracePart a, TracePart b) {
			// If both are null, or both are same instance, return true
			if ( System.Object.ReferenceEquals (a, b) )
				return (true) ;
			// If one is null, but not both, return false
			if ( ((object)a == null) || ((object)b == null) )
				return (false) ;
			// Return true if the fields match
			return (a.Category == b.Category && a.Info == b.Info) ;
		}

		public static bool operator != (TracePart a, TracePart b) {
			return (!(a == b)) ;
		}

	}

	public abstract class TraceListenerBase : TraceListener {
		public List<TracePart> _parts =new List<TracePart> () ;
		protected bool _closedPart =true ;

		protected TraceListenerBase () : base () {
		}

		#region Overrides
		public override void Close () {
		}

		public override void Flush () {
			if ( _closedPart == false )
				AddClosedNode ("", "") ;
		}

		public override void Write (object obj) {
			Write (obj.ToString ()) ;
		}

		public override void Write (string message) {
			AddOpenNode (message, "") ;
		}

		public override void Write (object obj, string category) {
			Write (obj.ToString (), category) ;
		}

		public override void Write (string message, string category) {
			AddOpenNode (message, category) ;
		}

		public override void WriteLine (object obj) {
			WriteLine (obj.ToString ()) ;
		}

		public override void WriteLine (string message) {
			AddClosedNode (message, "") ;
		}

		public override void WriteLine (object obj, string category) {
			WriteLine (obj.ToString (), category) ;
		}

		public override void WriteLine (string message, string category) {
			AddClosedNode (message, category) ;
		}

		#endregion

		#region Private Methods
		// Async handler for adding a node to a TreeView
		protected delegate void AddNodeDelegate (TracePart part) ;
		protected abstract void AppendNodeToControl (TracePart part) ;

		protected virtual void AddOpenNode (string message, string category) {
			if ( _closedPart == true || _parts.Count == 0 )
				_parts.Add (new TracePart () { Timestamp =DateTime.Now, IndentLevel =Trace.IndentLevel, Category =category, Info =message }) ;
			else
				_parts.Last ().Info +=message ;
			_closedPart =false ;
		}

		protected virtual void AddClosedNode (string message, string category) {
			if ( _closedPart == true || _parts.Count == 0 )
				_parts.Add (new TracePart () { Timestamp =DateTime.Now, IndentLevel =Trace.IndentLevel, Category =category, Info =message }) ;
			else
				_parts.Last ().Info +=message ;
			_closedPart =true ;
		}

		protected static string PrependIndent (int indentLevel, string message) {
			StringBuilder bldr =new StringBuilder () ;
			bldr.AppendFormat ("{0}{1}", IndentString (indentLevel), message);
			return (bldr.ToString ()) ;
		}

		protected static string IndentString (int indentLevel) {
			return (new String (' ', Trace.IndentSize * indentLevel)) ;
		}

		#endregion

	}

}
