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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Net;

using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;

namespace Autodesk.ADN.WpfReCap {

	// more to see progrees ?
	// http://stuff.seans.com/2009/01/05/using-httpwebrequest-for-asynchronous-downloads/

	public delegate void UploadPhotosCompletedDelegate (IRestResponse response) ;

	public partial class UploadProgress : Window {
		protected string _photosceneid ;
		public RestRequestAsyncHandle _asyncHandle ;
		public UploadPhotosCompletedDelegate _callback =null ;
		private IProgress<ProgressInfo> _progressIndicator ;

		protected UploadProgress () {
			InitializeComponent () ;
		}

		public UploadProgress (string photosceneid) {
			_photosceneid =photosceneid ;
			InitializeComponent () ;
		}

		#region Job Progress tasks
		private void ReportProgress (ProgressInfo value) {
			progressBar.Value =value.pct ;
			progressMsg.Content =value.msg ;
			progressBar.IsIndeterminate =(value.pct != 0 && value.pct != 100) ;
		}

		public void callback (IRestResponse response, RestRequestAsyncHandle asyncHandle) {
			if (   response.StatusCode != HttpStatusCode.OK
				|| response.Content.IndexOf ("<error>") != -1
				|| response.Content.IndexOf ("<Error>") != -1
			) {
				_progressIndicator.Report (new ProgressInfo (0, "UploadFiles error")) ;
			} else {
				_progressIndicator.Report (new ProgressInfo (100, "UploadFiles succeeded")) ;
			}
			this.Dispatcher.Invoke (_callback, new Object [] { response }) ;
		}

		#endregion

		#region Window events
		private void Window_Loaded (object sender, RoutedEventArgs e) {
			sceneid.Content =_photosceneid ;
			ReportProgress (new ProgressInfo (1, "Uploading files to the ReCap server...")) ;
			_progressIndicator =new Progress<ProgressInfo> (ReportProgress) ;
		}

		private void Button_Click (object sender, RoutedEventArgs e) {
			_asyncHandle.Abort () ;
		}

		#endregion

	}

}
