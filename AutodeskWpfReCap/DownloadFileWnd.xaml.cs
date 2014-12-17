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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web;
using System.ComponentModel;

namespace Autodesk.ADN.WpfReCap {

	public delegate void DownloadFileCompletedDelegate (string photoscene, string filename) ;

	public partial class DownloadFileWnd : Window {
		public DownloadFileCompletedDelegate _callback =null ;
		protected WebClient _webClient ;
		protected Stopwatch _sw =new Stopwatch () ;

		protected string _urlAddress { get; set; }
		protected string _location { get; set; }

		protected DownloadFileWnd () {
			InitializeComponent () ;
		}

		public DownloadFileWnd (string urlAddress, string location) {
			_urlAddress =urlAddress ;
			_location =location ;
			InitializeComponent () ;
		}

		private void Window_Loaded (object sender, RoutedEventArgs e) {
			DownloadFile (_urlAddress, _location) ;
		}

		public void DownloadFile (string urlAddress, string location) {
			url.Content =urlAddress ;
			project.Content =System.IO.Path.GetFileNameWithoutExtension (location) ;
			using ( _webClient =new WebClient () ) {
				_webClient.DownloadFileCompleted +=new AsyncCompletedEventHandler (Completed) ;
				_webClient.DownloadProgressChanged +=new DownloadProgressChangedEventHandler (ProgressChanged) ;
				try {
					_sw.Reset () ;
					_sw.Start () ;
					_webClient.DownloadFileAsync (new Uri (urlAddress), location) ;
				} catch ( Exception ex ) {
					MessageBox.Show (ex.Message) ;
				}
			}
		}

		private void ProgressChanged (object sender, DownloadProgressChangedEventArgs e) {
			// Calculate download speed and output it to labelSpeed.
			speed.Content =string.Format ("{0} kb/s", (e.BytesReceived / 1024d / _sw.Elapsed.TotalSeconds).ToString ("0.00")) ;
			// Update the progressbar percentage only when the value is not the same.
			progressBar.Value =e.ProgressPercentage ;
			// Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
			sizes.Content =string.Format ("{0} Mb / {1} Mb",
				(e.BytesReceived / 1024d / 1024d).ToString ("0.00"),
				(e.TotalBytesToReceive / 1024d / 1024d).ToString ("0.00")
			) ;
		}

		// The event that will trigger when the WebClient is completed
		private void Completed (object sender, AsyncCompletedEventArgs e) {
			_sw.Stop () ;
			//if ( e.Cancelled == true ) {
			//	MessageBox.Show ("Download has been canceled.") ;
			//} else {
			//	MessageBox.Show ("Download completed!") ;
			//}
			_webClient =null ;
			string photosceneid =System.IO.Path.GetFileNameWithoutExtension (_location) ;
			if ( e.Cancelled == false && _callback != null )
				this.Dispatcher.Invoke (_callback, new Object [] { photosceneid, _location }) ;
			else if ( e.Cancelled == true )
				File.Delete (_location) ;
			this.Close () ;
		}

		private void Button_Click (object sender, RoutedEventArgs e) {
			_webClient.CancelAsync () ;
		}

		private void Button_Unloaded (object sender, RoutedEventArgs e) {
			if ( _webClient != null && _webClient.IsBusy ) {
				_webClient.CancelAsync () ;
			}
		}

	}

}
