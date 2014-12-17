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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Collections.Specialized;

// http://www.drdobbs.com/windows/creating-trace-listeners-in-net/184405895
// http://www.c-sharpcorner.com/UploadFile/mahesh/wpf-richtextbox/

namespace Autodesk.ADN.Toolkit.Wpf.RestLogger {

	public delegate void UpdatedDelegate () ;

	public partial class RestLoggerWnd : Window {
		public UpdatedDelegate Updated =null ;

		public RestLoggerWnd () {
			InitializeComponent () ;
		}

		private void Window_Initialized (object sender, EventArgs e) {
			Trace.Listeners.Add (new TreeViewTraceListener (treeLogger)) ;
			Trace.Listeners.Add (new ListViewTraceListener (gridLogger));
			Trace.Listeners.Add (new TextBoxTraceListener (textLogger));
		}
	
		private void Window_Loaded (object sender, RoutedEventArgs e) {
		}

		private void Window_Closed (object sender, System.EventArgs e) {
			Trace.Listeners.Clear () ;
		}

		// We only need one event as the 3 controls are updated together
		private void textLogger_TextChanged (object sender, TextChangedEventArgs e) {
			if ( Updated != null )
				this.Dispatcher.Invoke (Updated) ;
		}

	}



}
