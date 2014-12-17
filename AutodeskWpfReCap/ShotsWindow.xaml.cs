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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Xml;
using System.ComponentModel;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Windows.Resources;
using System.Resources;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;

using Autodesk.ADN.Toolkit.Wpf.Viewer;
using Autodesk.ADN.Toolkit.ReCap;

namespace Autodesk.ADN.WpfReCap {

	public class ReCapPhotoItem {
		public string Name { get; set; }
		public string Type { get; set; }
		public string Image { get; set; }
	}

	public partial class ShotsWindow : Window {
		protected AdskReCap _recap =null ;
		protected string _photosceneid =null ;

		protected ShotsWindow () {
			InitializeComponent () ;
			Thumbnails.View =Thumbnails.FindResource ("tileView") as ViewBase ;
		}

		public ShotsWindow (string photosceneid) {
			_photosceneid =photosceneid ;
			InitializeComponent () ;
			Thumbnails.View =Thumbnails.FindResource ("tileView") as ViewBase ;
			Thumbnails.ItemsSource =new ObservableCollection<ReCapPhotoItem> () ;
		}

		#region Window events
		// In debug, Drag'nDrop will not work if you run Developer Studio as administrator
		private void Thumbnails_Drop (object sender, DragEventArgs e) {
			e.Handled =true ;
			if ( e.Data.GetDataPresent (DataFormats.FileDrop) ) {
				ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> ((IEnumerable<ReCapPhotoItem>)Thumbnails.ItemsSource) ;
				string [] files =(string [])e.Data.GetData (DataFormats.FileDrop) ;
				foreach ( string filename in files ) {
					items.Add (new ReCapPhotoItem () {
						Name =System.IO.Path.GetFileNameWithoutExtension (filename),
						Type =System.IO.Path.GetExtension (filename),
						Image =filename
					}) ;
				}
				Thumbnails.ItemsSource =items ;
				Thumbnails.SelectAll () ;
			}
		}

		#endregion

		#region Examples
		// Example getting images with wildcard search on local disc
		/*private void Tirelire_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> () ;
			DirectoryInfo folder =new DirectoryInfo (@"C:\Program Files\Autodesk\AutodeskWpfReCap\Images") ;
			FileInfo [] images =folder.GetFiles ("Tirelire*.jpg") ;
			foreach ( FileInfo img in images ) {
				items.Add (new ReCapPhotoItem () { Name =img.Name, Type =img.Extension, Image =img.FullName }) ;
				//new BitmapImage (new Uri (img.FullName))
			}
			Thumbnails.ItemsSource =items ;
			Thumbnails.SelectAll () ;
		}*/

		// Example taking images from the Application Resource
		private void Tirelire_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> () ;
			for ( int i =0 ; i < 6 ; i++ ) {
				items.Add (new ReCapPhotoItem () {
					Name ="Tirelire" + i.ToString (),
					Type ="jpg",
					Image =@"Images\Tirelire" + i.ToString () + ".jpg"
				}) ;
			}
			Thumbnails.ItemsSource =items ;
			Thumbnails.SelectAll () ;
		}

		// Examples referencing images on the WEB
		private void KidSnail_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> () ;
			for ( int i =0 ; i < 63 ; i++ ) {
				items.Add (new ReCapPhotoItem () {
					Name ="KidSnail" + i.ToString (),
					Type ="jpg",
					Image =@"https://raw.github.com/ADN-DevTech/Autodesk-ReCap-Samples/master/Examples/KidSnail/KidSnail" + i.ToString () + ".jpg"
				}) ;
			}
			Thumbnails.ItemsSource =items ;
			Thumbnails.SelectAll () ;
		}

		private void Calc_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> () ;
			for ( int i =0 ; i < 60 ; i++ ) {
				items.Add (new ReCapPhotoItem () {
					Name ="Calc" + i.ToString (),
					Type ="jpg",
					Image =@"https://raw.github.com/ADN-DevTech/Autodesk-ReCap-Samples/master/Examples/Calc/Calc" + i.ToString () + ".jpg"
				}) ;
				}
			Thumbnails.ItemsSource =items ;
			Thumbnails.SelectAll () ;
		}

		// Example getting images with images from a ZIP file on local disc
		// These examples come from the ReCap offical site that need to be downloaded first
		// as ReCap API cannot reference a ZIP file, or images in ZIP
		private void GetReCapExample (string url, string location) {
			if ( !File.Exists (location) ) {
				if ( url != null ) {
					if ( System.Windows.MessageBox.Show ("This sample is quite large, are you sure you want to proceed?\nThe file would be downloaded only once.", "ReCap Example download", MessageBoxButton.YesNo) == MessageBoxResult.Yes )
						ReCapExample_Download (url, location) ;
				}
				return ;
			}

			ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> () ;
			FileStream zipStream =File.OpenRead (location) ;
			using ( ZipArchive zip =new ZipArchive (zipStream) ) {
				foreach ( ZipArchiveEntry entry in zip.Entries ) {
					items.Add (new ReCapPhotoItem () {
						Name =System.IO.Path.GetFileNameWithoutExtension (entry.Name),
						Type =System.IO.Path.GetExtension (entry.Name).Trim (new char [] { '.' }),
						Image =location + ":" + entry.FullName 
					}) ;
				}
			}
			Thumbnails.ItemsSource =items ;
			Thumbnails.SelectAll () ;
		}

		private void ReCapExample_Download (string url, string location) {
			DownloadFileWnd wnd =new DownloadFileWnd (url, location) ;
			wnd._callback =new DownloadFileCompletedDelegate (this.DownloadExampleCompleted) ;
			wnd.Owner =this ;
			wnd.Show () ;
		}

		public void DownloadExampleCompleted (string zip, string filename) {
			GetReCapExample (null, filename) ;
		}

		private void Warrior_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			string url ="https://360.autodesk.com/Public/Download?hash=e0c8c37990674a24a561ba365009f5f4" ;
			string location =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + "Warrior.zip" ;
			GetReCapExample (url, location) ;
		}

		private void Horns_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			string url ="https://360.autodesk.com/Public/Download?hash=8eca9c8f22f8458b9ea35cec2e1dc7e3" ;
			string location =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + "Horns.zip" ;
			GetReCapExample (url, location) ;
		}

		private void Alligator_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			string url ="https://360.autodesk.com/Public/Download?hash=3e15e3b1064f41ff823d7e05ff8cae9b" ;
			string location =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + "Alligator.zip" ;
			GetReCapExample (url, location) ;
		}

		private void Mask_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			string url ="https://360.autodesk.com/Public/Download?hash=3e152c36f6e6438581b36e7c9f9eed5f" ;
			string location =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + "Mask.zip" ;
			GetReCapExample (url, location) ;
		}

		private void GymCenter_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			string url ="https://360.autodesk.com/Public/Download?hash=f92f89c676a7419a8e8bf9040a3280c7" ;
			string location =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + "GymCenter.zip" ;
			GetReCapExample (url, location) ;
		}

		private void Marriot_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;
			string url ="https://360.autodesk.com/Public/Download?hash=bb90ed0616ea4078b9ef4da27f8fa975" ;
			string location =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + "Marriot.zip" ;
			GetReCapExample (url, location) ;
		}

		#endregion

		#region UI - Commands
		private void Thumbnails_Remove (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			foreach ( ReCapPhotoItem item in Thumbnails.SelectedItems )
				Thumbnails.Items.Remove (item) ;
			Thumbnails.Items.Refresh () ;
		}

		private void Thumbnails_RemoveAll (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			Thumbnails.ItemsSource =null ;
			Thumbnails.Items.Refresh () ;
		}

		private async void Thumbnails_UploadPhotos (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( Thumbnails.SelectedItems.Count == 0 /*|| Thumbnails.SelectedItems.Count > 20*/ ) {
				//System.Windows.MessageBox.Show ("No images selected, or too many images selected (max 20 in one upload)!") ;
				System.Windows.MessageBox.Show ("No images selected!") ;
				return ;
			}
			await UploadPhotos (_photosceneid) ;
		}

		private void Thumbnails_SelectAll (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			Thumbnails.SelectAll () ;
		}

		private async void Thumbnails_DownloadPhotos (object sender, RoutedEventArgs e) {
			if ( !await PhotosceneProperties (_photosceneid) )
				return ;

			Thumbnails.ItemsSource =new ObservableCollection<ReCapPhotoItem> () ;
			dynamic response =_recap.response () ;
			dynamic files =response.Photoscenes.Photoscene.Files ;
			string dlFolder =System.IO.Path.GetFullPath (AppDomain.CurrentDomain.BaseDirectory) + response.Photoscenes.Photoscene.photosceneid ;
			if ( !Directory.Exists (dlFolder) )
				Directory.CreateDirectory (dlFolder) ;
			foreach ( KeyValuePair<string, object> pair in files.Dictionary ) {
				dynamic fnode =pair.Value ;
				AdskReCap.FileType type =(AdskReCap.FileType)Enum.Parse (typeof (AdskReCap.FileType), fnode.type, true) ;
				if ( fnode.fileid == "" || type != AdskReCap.FileType.Image )
					continue ;
				string location =dlFolder + @"\" + fnode.filename ;
				if ( File.Exists (location) ) {
					ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> ((IEnumerable<ReCapPhotoItem>)Thumbnails.ItemsSource) ;
					items.Add (new ReCapPhotoItem () {
						Name =System.IO.Path.GetFileNameWithoutExtension (fnode.filename),
						Type =System.IO.Path.GetExtension (fnode.filename),
						Image =location
					}) ;
					Thumbnails.ItemsSource =items ;
					continue ;
				}

				if ( !await _recap.GetFile (fnode.fileid, type) )
					continue ;
				dynamic fileResponse =_recap.response () ;
				dynamic file =fileResponse.Files.file ;
				string link =file.filelink ;

				DownloadFileWnd wnd =new DownloadFileWnd (link, location) ;
				wnd._callback =new DownloadFileCompletedDelegate (this.DownloadFileCompleted) ;
				wnd.Owner =this ;
				wnd.Show () ;
			}
		}

		public void DownloadFileCompleted (string img, string filename) {
			ObservableCollection<ReCapPhotoItem> items =new ObservableCollection<ReCapPhotoItem> ((IEnumerable<ReCapPhotoItem>)Thumbnails.ItemsSource) ;
			items.Add (new ReCapPhotoItem () {
				Name =img,
				Type =System.IO.Path.GetExtension (filename),
				Image =filename
			}) ;
			Thumbnails.ItemsSource =items ;
		}

		public void Thumbnails_Preview (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			foreach ( ReCapPhotoItem item in Thumbnails.SelectedItems ) {
				BitmapImage image =new BitmapImage (new Uri (item.Image)) ;

				ImagePreview preview =new ImagePreview () ;
				preview._imageURL =image ;
				preview.Owner =this.Owner ;
				preview.Show () ;
			}
		}

		#endregion

		#region ReCap Calls
		protected bool ConnectWithReCapServer () {
			if ( _recap != null )
				return (true) ;
			_recap =new AdskReCap (
				UserSettings.ReCapClientID,
				UserSettings.CONSUMER_KEY, UserSettings.CONSUMER_SECRET,
				Properties.Settings.Default.oauth_token, Properties.Settings.Default.oauth_token_secret
			) ;
			return (_recap != null) ;
		}

		protected async Task<bool> UploadPhotos (string photosceneid) {
			if ( photosceneid == "" || !ConnectWithReCapServer () )
				return (false) ;

			//- Collect images
			Dictionary<string, string> files =new Dictionary<string, string> () ;
			//Dictionary<string, string> filesRef =new Dictionary<string, string> () ;
			foreach ( ReCapPhotoItem item in Thumbnails.SelectedItems ) {
				//files.Add (item.Name, item.Image) ;
				if ( File.Exists (item.Image) ) {
					files.Add (item.Name, item.Image) ;
				} else if ( item.Image.Substring (0, 4).ToLower () == "http" || item.Image.Substring (0, 3).ToLower () == "ftp" ) {
					//filesRef.Add (item.Name, item.Image) ;
					files.Add (item.Name, item.Image) ;
				} else if ( item.Image.ToLower ().Contains (".zip:") == true ) {
 					// ReCap does not works with zip, we need to send images one by one
					string [] sts =item.Image.Split (':') ;
					if ( sts.Length == 3 ) {
						sts [1] =sts [0] + ":" + sts [1] ;
						sts =sts.Where (w => w != sts [0]).ToArray () ;
					}
					FileStream zipStream =File.OpenRead (sts [0]) ;
					using ( ZipArchive zip =new ZipArchive (zipStream) ) {
						ZipArchiveEntry entry =zip.GetEntry (sts [1]) ;
						DeflateStream str =entry.Open () as DeflateStream ;
						Byte [] byts =new Byte [entry.Length] ;
						str.Read (byts, 0, (int)entry.Length) ;
						files.Add (System.IO.Path.GetFileName (sts [1]), Convert.ToBase64String (byts)) ;
					}
				} else {
					// This is coming from our resources
					StreamResourceInfo stri =Application.GetResourceStream (new Uri (
						item.Image,
						UriKind.Relative
					)) ;
					if ( stri != null ) {
						Stream str =stri.Stream ;
						Byte [] byts =new Byte [str.Length] ;
						str.Read (byts, 0, (int)str.Length) ;
						files.Add (System.IO.Path.GetFileName (item.Image), Convert.ToBase64String (byts)) ;
					}
				}
			}

			// ReCap only accepts 20 uploads at a time with image not larger than 128Mb
			// Let's assume files size are ok and split calls by 20 max each time
			//     return (UploadPhotosExecute (photosceneid, files, filesRef)) ;
			int nRet =0 ;
			if ( files != null && files.Count != 0 ) {
				int i =0 ;
				int n =1 + files.Count / 20 ;
				nRet =+n ;
				var splits =(from item in files
							 group item by i++ % n into part
							 select part).ToList () ; // ToDictionary (g => g.Key, g => g.Last ());
				foreach ( var grp in splits ) {
					Dictionary<string, string> dict =new Dictionary<string, string> () ;
					foreach ( var entry in grp )
						dict.Add (entry.Key, entry.Value) ;
					if ( await UploadPhotosExecute (photosceneid, dict) )
						nRet-- ;
				}
			}
			return (nRet == 0) ;
		}

		protected async Task<bool> UploadPhotosExecute (string photosceneid, Dictionary<string, string> files) {
			// Synchronous sample
			//bool ret =_recap.UploadFiles (photosceneid, files) ;
			//if ( !ret ) {
			//	LogError ("UploadFiles error") ;
			//	return (false) ;
			//}
			//LogInfo ("UploadFiles succeeded") ;
			//dynamic doc =_recap.response () ;
			//dynamic nodes =response.Files ; // if json, do doc.Files.File
			//foreach ( KeyValuePair<string, object> pair in nodes.Dictionary ) {
			//	dynamic fnode =pair.Value ;
			//	LogInfo (string.Format ("\t{0} [{1}]", fnode.filename, fnode.fileid)) ;
			//}

			// Async call
			UploadProgress wnd =new UploadProgress (photosceneid) ;
			var asyncHandle =_recap.UploadFilesAsync (photosceneid, files, wnd.callback) ;
			if ( asyncHandle != null ) {
				LogInfo ("UploadFiles async successfully started") ;
				wnd._asyncHandle =asyncHandle ;
				wnd._callback =new UploadPhotosCompletedDelegate (this.UploadPhotosCompleted) ;
				wnd.Show () ;
			} else {
				LogError ("UploadFiles async error") ;
			}
			return (asyncHandle != null) ;
		}

		public void UploadPhotosCompleted (IRestResponse restResponse) {
			LogInfo (restResponse.Content) ;
			//if (   response.StatusCode != HttpStatusCode.OK
			//	|| response.Content.IndexOf ("<error>") != -1
			//	|| response.Content.IndexOf ("<Error>") != -1
			//) {
			//	LogError ("UploadFiles error") ;
			//	return ;
			//}
			//LogInfo ("UploadFiles succeeded") ;
			//XmlDocument doc =new XmlDocument () ;
			//doc.LoadXml (response.Content) ;
			//XmlNodeList nodes =doc.SelectNodes ("/Response/Files/file") ;
			//foreach ( XmlNode fnode in nodes ) {
			//	XmlNode p1 =fnode.SelectSingleNode ("filename") ;
			//	XmlNode p2 =fnode.SelectSingleNode ("fileid") ;
			//	LogInfo (string.Format ("\t{0} [{1}]", p1.InnerText, p2.InnerText)) ;
			//}
			_recap._lastResponse =restResponse ;
			if ( !_recap.isOk () ) {
				LogError ("UploadFiles error") ;
				return ;
			}
			LogInfo ("UploadFiles succeeded") ;
			dynamic response =_recap.response () ;
			dynamic nodes =response.Files ; // if json, do doc.Files.File
			foreach ( KeyValuePair<string, object> pair in nodes.Dictionary ) {
				dynamic fnode =pair.Value ;
				LogInfo (string.Format ("\t{0} [{1}]", fnode.filename, fnode.fileid)) ;
			}
		}

		protected async Task<bool> PhotosceneProperties (string photosceneid) {
			if ( photosceneid == "" || !ConnectWithReCapServer () )
				return (false) ;

			// Photoscene Properties
			LogInfo ("Photoscene Properties", LogIndent.PostIndent) ;
			bool bRet =await _recap.SceneProperties (photosceneid) ;
			Log (bRet ? "Photoscene Properties returned" : "PhotosceneProperties failed", LogIndent.PostUnindent, bRet ? "Info" : "Error") ;
			return (bRet) ;
		}

		#endregion

		#region Logger Window
		protected enum LogIndent { None = 0, PreIndent = 1, PreUnindent = -1, PostIndent = 2, PostUnindent = -2 } ;
		private static void LogInfo (string msg, LogIndent indent = LogIndent.None) {
			Log (msg, indent, "Info");
		}

		private static void LogError (string msg, LogIndent indent = LogIndent.None) {
			Log (msg, indent, "Error");
		}

		private static void Log (string msg, LogIndent indent = LogIndent.None, string category = "Info") {
			if ( Math.Abs ((int)indent) == 1 )
				Trace.IndentLevel += (int)indent;
			if ( msg != "" )
				Trace.WriteLine ("WpfRecap: " + msg, category);
			if ( Math.Abs ((int)indent) == 2 )
				Trace.IndentLevel += ((int)indent) / 2;
		}

		#endregion

	}

}