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
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Autodesk.ADN.Toolkit.Wpf.RestLogger {

	public class ListViewTraceListener : TraceListenerBase {
		public ListView Ctrl { get; set; }

		protected ListViewTraceListener () : base () {
		}

		public ListViewTraceListener (ListView ctrl) : base () {
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
				try {
					//ObservableCollection<TracePart> lvList =new ObservableCollection<TracePart> () ;
					//Ctrl.ItemsSource =lvList ;
					int pos =Ctrl.Items.Add (part) ;
				} catch /*( Exception ex )*/ {
				} finally {
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
