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

	public class TreeViewTraceListener : TraceListenerBase {
		public TreeView Ctrl { get; set; }
		private SolidColorBrush _redBrush =new SolidColorBrush (Color.FromRgb (0xee, 0x66, 0x33)) ;
		private SolidColorBrush _yellowBrush =new SolidColorBrush (Color.FromRgb (0xff, 0xee, 0x66)) ;
		private SolidColorBrush _brownBrush =new SolidColorBrush (Color.FromRgb (0xb4, 0x44, 0x00)) ;

		protected TreeViewTraceListener () : base () {
		}

		public TreeViewTraceListener (TreeView ctrl) : base () {
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
					// Move to the correct node collection given the current node and the depth
					ItemCollection coll =MoveToDepth (part.IndentLevel) ;

					// Add the new message					
					TreeViewItem child =new TreeViewItem () ;
					child.Header =part.ToString () ;
					if ( part.Category.ToLowerInvariant () == "error" || part.Category.ToLowerInvariant () == "exception" ) {
						//child.Background =_redBrush ;
						//child.Foreground =Brushes.White ;
						child.Foreground =_redBrush ;
					} else if ( part.Category.ToLowerInvariant () == "warning" ) {
						//child.Background =_yellowBrush ;
						//child.Foreground =_brownBrush ;
						child.Foreground =_yellowBrush ;
					}
					coll.Add (child) ;

					TreeViewItem parent =child.Parent as TreeViewItem ;
					while ( parent != null ) {
						parent.IsExpanded =true ;
						parent =parent.Parent as TreeViewItem ;
					}
					child.BringIntoView () ;
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

		// Gets the last node of a collection
		private TreeViewItem GetLastTreeNode (ItemCollection coll) {
			if ( coll.Count == 0 )
				return (null) ;
			// Return the last element in this collection
			return (coll [coll.Count - 1] as TreeViewItem) ;
		}

		// Move to the appropriate node
		private ItemCollection MoveToDepth (int depth) {
			// Start at the root (depth = 0)
			int nActual =0 ;
			ItemCollection coll =Ctrl.Items ;

			// If the actual depth is less than our desired depth
			while ( nActual < depth ) {
				// We need to go to the next level so get the last node at this level
				TreeViewItem last =GetLastTreeNode (coll) ;
				if ( last == null ) {
					// There are no nodes at this level so create a new node
					last =new TreeViewItem () ;
					coll.Add (last) ;
				}

				// Move to the next level
				++nActual ;
				coll =last.Items ;
			}

			// Finally we're at the correct depth, return the collection
			return (coll) ;
		}

		#endregion

	}

}
