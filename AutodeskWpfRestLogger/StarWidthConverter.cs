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
using System.Windows.Data;

namespace Autodesk.ADN.Toolkit.Wpf.RestLogger {

	public class StarWidthConverter : IValueConverter {

		public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			ListView listview =value as ListView ;
			double width =listview.Width ;
			GridView gv =listview.View as GridView ;
			for ( int i =0 ; i < gv.Columns.Count ; i++ ) {
				if ( !Double.IsNaN (gv.Columns [i].Width) )
					width -=gv.Columns [i].Width ;
			}
			return (width - 5) ; // This is to take care of margin/padding
		}

		public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			return (null) ;
		}

	}

}
