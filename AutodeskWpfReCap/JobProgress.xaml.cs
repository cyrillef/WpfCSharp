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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.Xml;
using System.Diagnostics;

using Autodesk.ADN.Toolkit.ReCap;

namespace Autodesk.ADN.WpfReCap {

	public delegate void ProcessPhotosceneCompletedDelegate (string photoscene, string status) ;

	public partial class JobProgress : Window {
		public ProcessPhotosceneCompletedDelegate _callback =null ;
		protected CancellationTokenSource _cts =null ;
		protected AdskReCap _recap =null ;
		protected string _photosceneid ;

		protected JobProgress () {
			InitializeComponent () ;
		}

		public JobProgress (string photosceneid) {
			_photosceneid =photosceneid ;
			InitializeComponent () ;
		}

		#region Job Progress tasks
		private void ReportProgress (ProgressInfo value) {
			progressBar.Value =value.pct ;
			progressMsg.Content =value.msg ;
		}

		private async Task ReCapJobProgress (string photosceneid, IProgress<ProgressInfo> progress, CancellationToken ct, TaskScheduler uiScheduler) {
			progress.Report (new ProgressInfo (0, "Initializing...")) ;
			while ( !ct.IsCancellationRequested ) {
				//Task<ProgressInfo> task =Task<ProgressInfo>.Factory.StartNew (() => PhotosceneProgress (photosceneid)) ;
				//await task ;
				
				//if ( task.Result == null ) {
				//	progress.Report (new ProgressInfo (0, "Error")) ;
				//	break ;
				//}
				//progress.Report (task.Result) ;
				//if ( task.Result.pct >= 100 ) {
				//	this.Dispatcher.Invoke (_callback, new Object [] { _photosceneid, task.Result.msg }) ;
				//	break ;
				//}

				ProgressInfo info =await PhotosceneProgress (photosceneid) ;
				//var task =Task.Factory.StartNew (
				//	async delegate {
				//		ProgressInfo info =await PhotosceneProgress (photosceneid) ;
				//		progress.Report (info) ;
				//		if ( info.pct >= 100 ) {
				//			this.Dispatcher.Invoke (_callback, new Object [] { _photosceneid, info.msg }) ;

				//		}
				//		return (info) ;
				//	},
				//	CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default
				//) ;
				//await task ;

				if ( info == null ) {
					progress.Report (new ProgressInfo (0, "Error")) ;
					break ;
				}
				progress.Report (info) ;
				if ( info.pct >= 100 ) {
					if ( _callback != null )
						this.Dispatcher.Invoke (_callback, new Object [] { _photosceneid, info.msg }) ;
					break ;
				}
			}
		}

		#endregion

		#region Window events
		private async void Window_Loaded (object sender, RoutedEventArgs e) {
			sceneid.Content =_photosceneid ;
			if ( !ConnectWithReCap () )
				return ;

			TaskScheduler uiScheduler =TaskScheduler.FromCurrentSynchronizationContext () ;
			var progressIndicator =new Progress<ProgressInfo> (ReportProgress) ;
			_cts =new CancellationTokenSource () ;
			try {
				await ReCapJobProgress (_photosceneid, progressIndicator, _cts.Token, uiScheduler) ;
			} catch ( OperationCanceledException ex ) {
				Trace.WriteLine (ex.Message, "Exception") ;
			}
		}

		private void Button_Click (object sender, RoutedEventArgs e) {
			_cts.Cancel () ;
		}

		#endregion

		#region ReCap Calls
		protected bool ConnectWithReCap () {
			if ( _recap != null )
				return (true) ;
			_recap =new AdskReCap (
				UserSettings.ReCapClientID,
				UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
				Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret
			) ;
			return (_recap != null) ;
		}

		protected async Task<ProgressInfo> PhotosceneProgress (string photosceneid) {
			if ( !ConnectWithReCap () )
				return (null) ;

			if ( !await _recap.SceneProgress (photosceneid) )
				return (null) ;
			int pct =0 ;
			string msg ="" ;
			dynamic response =_recap.response () ;
			try {
				pct =(int)Convert.ToDouble (response.Photoscene.progress) ;
				msg =response.Photoscene.progressmsg ;
			} catch {
			}
			return (new ProgressInfo (pct, msg)) ;
		}

		#endregion

	}

	public class ProgressInfo {

		public int pct { get; set; }
		public string msg { get; set; }

		public ProgressInfo (int pctValue, string msgValue) {
			pct =pctValue ;
			msg =msgValue ;
		}

	}

}
