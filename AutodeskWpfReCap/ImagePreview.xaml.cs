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

namespace Autodesk.ADN.WpfReCap {

	public partial class ImagePreview : Window {

		public ImageSource _imageURL { get; set; }

		public ImagePreview () {
			InitializeComponent () ;
		}

		private void Window_Loaded (object sender, RoutedEventArgs e) {
			this.currentImage.Source =_imageURL ;
			this.opSlider.Value =1.0 ;
		}

		#region Selection
		private Point _mouseDownPos ; // The point where the mouse button was clicked down.

		private void Grid_MouseDown (object sender, MouseButtonEventArgs e) {
			Mouse.Capture (pnlImage, CaptureMode.Element) ;
			_mouseDownPos =Mouse.GetPosition (currentImage) ; // e.GetPosition (currentImage) ;

			// Initial placement of the drag selection box.         
			Canvas.SetLeft (selectionBox, _mouseDownPos.X) ;
			Canvas.SetTop (selectionBox, _mouseDownPos.Y) ;
			selectionBox.Width =0 ;
			selectionBox.Height =0 ;
			selectionBox.Visibility =Visibility.Visible ;
		}

		private void Grid_MouseMove (object sender, MouseEventArgs e) {
			Point pos =Mouse.GetPosition (pnlImage) ;
			if ( _mouseDownPos.X < pos.X ) {
				Canvas.SetLeft (selectionBox, _mouseDownPos.X) ;
				selectionBox.Width =pos.X - _mouseDownPos.X ;
			} else {
				Canvas.SetLeft (selectionBox, pos.X) ;
				selectionBox.Width =_mouseDownPos.X - pos.X ;
			}
			if ( _mouseDownPos.Y < pos.Y ) {
				Canvas.SetTop (selectionBox, _mouseDownPos.Y) ;
				selectionBox.Height =pos.Y - _mouseDownPos.Y ;
			} else {
				Canvas.SetTop (selectionBox, pos.Y) ;
				selectionBox.Height =_mouseDownPos.Y - pos.Y ;
			}
		}

		private void Grid_MouseUp (object sender, MouseButtonEventArgs e) {
			Point mouseUpPos =Mouse.GetPosition (pnlImage) ; // e.GetPosition (currentImage) ;
			Mouse.Capture (pnlImage, CaptureMode.None) ;
			selectionBox.Visibility =Visibility.Collapsed ;

			// TODO: 
			//
			// The mouse has been released, check to see if any of the items 
			// in the other canvas are contained within mouseDownPos and 
			// mouseUpPos, for any that are, select them!
			//
		}

		#endregion

	}

}
