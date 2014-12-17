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
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace Autodesk.ADN.Toolkit.Wpf.Viewer {

	public abstract class Adsk3dNavigate {

		protected Point _lastPos ;
		protected Vector3D _lastPos3D ;
		protected Viewport3D _viewport ;
		protected PerspectiveCamera _camera ;

		public Point Position { get { return (_lastPos) ; } set { _lastPos =value ; _lastPos3D =Make3d (_lastPos) ; } }
		public Vector3D Translation { get; internal set; }
		public Quaternion Rotation { get; internal set; }

		protected Adsk3dNavigate () {
		}

		public Adsk3dNavigate (Viewport3D viewport, PerspectiveCamera camera) {
			_viewport =viewport ;
			_camera =camera ;
			_lastPos3D =new Vector3D (1, 0, 0) ;
		}

		public virtual void Clear () {
			Translation =new Vector3D () ;
			Rotation =new Quaternion () ;
		}

		public abstract Vector3D Viewport_Pan (Point actualPos) ;
		public abstract Quaternion Viewport_Rotate (Point actualPos) ;
		protected abstract Vector3D Make3d (Point pos) ;

	}

}
