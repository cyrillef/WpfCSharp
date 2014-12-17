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

	public class AdskTrackball : Adsk3dNavigate {

		protected AdskTrackball () {
		}

		public AdskTrackball (Viewport3D viewport, PerspectiveCamera camera) : base (viewport, camera) {
		}

		public override Vector3D Viewport_Pan (Point actualPos) {
			Vector3D pos3D =Make3d (actualPos) ;

			//Length(original_position - cam_position) / Length(offset_vector) = Length(zNearA - cam_position) / Length(zNearB - zNearA)
			//offset_vector = Length(original_position - cam_position) / Length(zNearA - cam_position) * (zNearB - zNearA)
			double halfFOV =(_camera.FieldOfView / 2.0f) * (Math.PI / 180.0) ;
			double distanceToObject =((Vector3D)_camera.Position).Length ; // Compute the world space distance from the camera to the object you want to pan
			double projectionToWorldScale =distanceToObject * Math.Tan (halfFOV) ;
			Vector mouseDeltaInScreenSpace =actualPos - _lastPos ; // The delta mouse in pixels that we want to pan
			Vector mouseDeltaInProjectionSpace =new Vector (mouseDeltaInScreenSpace.X * 2 / _viewport.ActualWidth, mouseDeltaInScreenSpace.Y * 2 / _viewport.ActualHeight) ; // ( the "*2" is because the projection space is from -1 to 1)
			Vector cameraDelta =-mouseDeltaInProjectionSpace * projectionToWorldScale ; // Go from normalized device coordinate space to world space (at origin)

			Vector3D tr =new Vector3D (0.0d, -cameraDelta.Y, -cameraDelta.X) ; // Remember we are up=<0,-1,0>
			Translation +=tr ;

			_lastPos =actualPos ;
			_lastPos3D =pos3D ;

			return (tr) ;
		}

		public override Quaternion Viewport_Rotate (Point actualPos) {
			Vector3D pos3D =Make3d (actualPos) ;
			Vector3D axis =Vector3D.CrossProduct (_lastPos3D, pos3D) ;
			double angle =Vector3D.AngleBetween (_lastPos3D, pos3D) ;

			if ( axis.Length == 0 || angle == 0 )
				return (Quaternion.Identity) ;
			Quaternion quat =new Quaternion (axis, -angle) ;
			Rotation =quat * Rotation ;

			_lastPos =actualPos ;
			_lastPos3D =pos3D ;

			return (quat) ;
		}

		// http://scv.bu.edu/documentation/presentations/visualizationworkshop08/materials/opengl/trackball.c
		// http://curis.ku.dk/ws/files/38552161/01260772.pdf
		protected override Vector3D Make3d (Point pos) { // Project an <x, y> pair onto a sphere (ProjectToTrackball)
			// Translate 0,0 to the center, so <x, y> is [<-1, -1> - <1, 1>]
			double x =pos.X / (_viewport.ActualWidth / 2) - 1 ;
			double y =1 - pos.Y / (_viewport.ActualHeight / 2) ; // Flip Y - up instead of down
			double z2 =1 - Math.Pow (x, 2) - Math.Pow (y, 2) ; // z^2 =1 - x^2 - y^2
			double z =(z2 > 0 ? Math.Sqrt (z2) : 0) ;
			// Remember we are up=<0,-1,0>
			return (new Vector3D (-z, -y, x)) ;
		}

	}

}
