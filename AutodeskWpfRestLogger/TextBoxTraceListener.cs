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
using System.Windows.Media;
using System.Windows.Documents;
using System.Diagnostics;

namespace Autodesk.ADN.Toolkit.Wpf.RestLogger {

	public class TextBoxTraceListener : TraceListenerBase {
		public TextBoxBase Ctrl { get; set; }
		private SolidColorBrush _redBrush =new SolidColorBrush (Color.FromRgb (0xee, 0x66, 0x33)) ;
		private SolidColorBrush _yellowBrush =new SolidColorBrush (Color.FromRgb (0xff, 0xee, 0x66)) ;
		private SolidColorBrush _brownBrush =new SolidColorBrush (Color.FromRgb (0xb4, 0x44, 0x00)) ;

		protected TextBoxTraceListener () : base () {
		}

		public TextBoxTraceListener (TextBoxBase ctrl) : base () {
			Ctrl =ctrl ;
		}

		#region Overrides
		public override void Close () {
			Ctrl =null ;
		}

		#endregion

		#region Private Methods
		protected override void AddOpenNode (string message, string category) {
			base.AddOpenNode (message, category) ;
		}

		protected override void AddClosedNode (string message, string category) {
			base.AddClosedNode (message, category) ;
			AppendNodeToControl (_parts.Last ()) ;
		}

		protected override void AppendNodeToControl (TracePart part) {
			if ( Ctrl == null )
				return ;
			if ( Ctrl.Dispatcher.CheckAccess () ) {
				string st ="[" + part.Timestamp.ToString ("T") + "] " + part.Category + (part.Category != "" ? ": " : "") + IndentString (part.IndentLevel) + part.Info ;
				RichTextBox richCtrl =Ctrl as RichTextBox ;
				if ( richCtrl != null ) {
					//Paragraph pr =new Paragraph (new Run (part.ToString ())) ;
					//pr.TextIndent =part.IndentLevel * Trace.IndentSize * 5 ;
					Paragraph pr =new Paragraph (new Run (st)) ;
					if ( part.Category.ToLowerInvariant () == "error" ) {
						pr.Background =_redBrush ;
						pr.Foreground =Brushes.White ;
					} else if ( part.Category.ToLowerInvariant () == "warning" ) {
						pr.Background =_yellowBrush ;
						pr.Foreground =_brownBrush ;
					}
					richCtrl.Document.Blocks.Add (pr) ;
					richCtrl.ScrollToEnd () ;
				} else {
					//Ctrl.AppendText (part.ToString () + "\r\n") ;
					Ctrl.AppendText (st + "\r\n") ;
					Ctrl.ScrollToEnd () ;
				}
			} else {
				Ctrl.Dispatcher.Invoke (
					System.Windows.Threading.DispatcherPriority.Normal,
					new AddNodeDelegate (this.AppendNodeToControl),
					/*new object [] { part, }*/part
				) ;
			}
		}

		#endregion

	}

}
