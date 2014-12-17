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

// http://helixtoolkit.codeplex.com/license
// The MIT License (MIT)
// Copyright (c) 2012 Oystein Bjorke
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Autodesk.ADN.Toolkit.Wpf.Viewer {

	public class WireframeMeshAdapter {
		protected ModelVisual3D _model ;
		protected Matrix3D _screenToVisual ;
		protected Matrix3D _visualToScreen ;
		protected Matrix3D _visualToProjection ;
		protected Matrix3D _projectionToScreen ;
		protected Viewport3D _viewport ;

		protected Point3DCollection Points ;
		protected Int32Collection Indices ;
		protected double _thickness =1.0 ;
		protected double _depthOffset =0.0 ;
		protected Color _color ;

		protected WireframeMeshAdapter (ModelVisual3D model) {
			_model =model ;
			_color =Color.FromRgb (67, 255, 163) ; // Maya color
		}

		public static ModelVisual3D BuildVisualModel (ModelVisual3D objmesh, double thickness =1.0, double depthOffset =0.0) {
			WireframeMeshAdapter adp =new WireframeMeshAdapter (objmesh) ;
			adp._thickness =thickness ;
			adp._depthOffset =depthOffset ;
			adp.UpdateTransforms () ;
			return (adp.MakeVisualModel ()) ;
		}

		protected MeshGeometry3D MakeGeometry () {
			var r =new MeshGeometry3D () ;
			BuildGeometry () ;
			r.Positions =Points ;
			r.TriangleIndices =Indices ;
			return (r) ;
		}

		protected Material MakeMaterial () {
			MaterialGroup matGroup =new MaterialGroup () ;
			matGroup.Children.Add (new DiffuseMaterial (new SolidColorBrush (_color))) ;
			return (matGroup) ;
		}

		protected Model3D MakeModel () {
			GeometryModel3D ret =new GeometryModel3D (MakeGeometry (), MakeMaterial ()) ;
			ret.BackMaterial =ret.Material ;
			return (ret) ;
		}

		protected ModelVisual3D MakeVisualModel () {
			var r =new ModelVisual3D () ;
			r.Content =MakeModel () ;
			Points.Freeze () ; // We won't change them anymore
			Indices.Freeze () ;

			r.Transform =new Transform3DGroup () ;
			Transform3DGroup transformGroup =r.Transform as Transform3DGroup ;
			Transform3DGroup meshTransformGroup =_model.Transform as Transform3DGroup ;
			transformGroup.Children.Add (meshTransformGroup.Children [0].Clone ()) ;
			transformGroup.Children.Add (meshTransformGroup.Children [1].Clone ()) ;
			return (r) ;
		}

		protected void BuildGeometry () {
			CreatePositionsAndIndices (_thickness, _depthOffset) ;
		}

		protected void CreateIndices () {
			Indices =new Int32Collection () ;
			int n =Points.Count ;
			for ( int i =0 ; i < n ; i +=4 ) {
				Indices.Add (i + 2) ;
				Indices.Add (i + 1) ;
				Indices.Add (i + 0) ;
				Indices.Add (i + 2) ;
				Indices.Add (i + 3) ;
				Indices.Add (i + 1) ;
			}
		}

		protected void CreatePositionsAndIndices (double thickness =1.0, double depthOffset =0.0) {
			Points =new Point3DCollection () ;
			GeometryModel3D geomModel =_model.Content as GeometryModel3D ;
			MeshGeometry3D mesh =geomModel.Geometry as MeshGeometry3D ;
			double halfThickness =0.5 * thickness ;
			int segmentCount =mesh.TriangleIndices.Count ;
			for ( int i =0 ; i < segmentCount ; i +=3 ) {
				var p0 =mesh.Positions [mesh.TriangleIndices [i]] ;
				var p1 =mesh.Positions [mesh.TriangleIndices [i + 1]] ;
				var p2 =mesh.Positions [mesh.TriangleIndices [i + 2]] ;

				Point3DCollection tmp =CreateSegment (p0, p1, halfThickness, depthOffset) ;
				foreach ( Point3D pt in tmp ) Points.Add (pt) ;
				tmp =CreateSegment (p1, p2, halfThickness, depthOffset) ;
				foreach ( Point3D pt in tmp ) Points.Add (pt) ;
				tmp =CreateSegment (p2, p0, halfThickness, depthOffset) ;
				foreach ( Point3D pt in tmp ) Points.Add (pt) ;
			}
			CreateIndices () ;
		}

		protected Point3DCollection CreateSegment (Point3D p0, Point3D p1, double halfThickness =0.5, double depthOffset =0.0) {
			Point3DCollection positions =new Point3DCollection () ;
			// Transform the start and end points to screen space
			var s0 =(Point4D)p0 * _visualToScreen ;
			var s1 =(Point4D)p1 * _visualToScreen ;
			var lx =(s1.X / s1.W) - (s0.X / s0.W) ;
			var ly =(s1.Y / s1.W) - (s0.Y / s0.W) ;
			var l2 =(lx * lx) + (ly * ly) ;
			var p00 =s0 ;
			var p01 =s0 ;
			var p10 =s1 ;
			var p11 =s1 ;
			if ( l2.Equals (0) ) {
				// coinciding points (in world space or screen space)
				var dz =halfThickness ;
				// TODO: make a square with the thickness as side length
				p00.X -=dz * p00.W ;
				p00.Y -=dz * p00.W ;
				p01.X -=dz * p01.W ;
				p01.Y +=dz * p01.W ;
				p10.X +=dz * p10.W ;
				p10.Y -=dz * p10.W ;
				p11.X +=dz * p11.W ;
				p11.Y +=dz * p11.W ;
			} else {
				var m =halfThickness / Math.Sqrt (l2) ;
				// the normal (dx,dy)
				var dx =-ly * m ;
				var dy =lx * m ;
				// segment start points
				p00.X +=dx * p00.W ;
				p00.Y +=dy * p00.W ;
				p01.X -=dx * p01.W ;
				p01.Y -=dy * p01.W ;
				// segment end points
				p10.X +=dx * p10.W ;
				p10.Y +=dy * p10.W ;
				p11.X -=dx * p11.W ;
				p11.Y -=dy * p11.W ;
			}
			if ( !depthOffset.Equals (0) ) {
				// Adjust the z-coordinate by the depth offset
				p00.Z -=depthOffset ;
				p01.Z -=depthOffset ;
				p10.Z -=depthOffset ;
				p11.Z -=depthOffset ;
				// Transform from screen space to world space
				p00 *=_screenToVisual ;
				p01 *=_screenToVisual ;
				p10 *=_screenToVisual ;
				p11 *=_screenToVisual ;
				positions.Add (new Point3D (p00.X / p00.W, p00.Y / p00.W, p00.Z / p00.W)) ;
				positions.Add (new Point3D (p01.X / p00.W, p01.Y / p01.W, p01.Z / p01.W)) ;
				positions.Add (new Point3D (p10.X / p00.W, p10.Y / p10.W, p10.Z / p10.W)) ;
				positions.Add (new Point3D (p11.X / p00.W, p11.Y / p11.W, p11.Z / p11.W)) ;
			} else {
				// Transform from screen space to world space
				p00 *=_screenToVisual ;
				p01 *=_screenToVisual ;
				p10 *=_screenToVisual ;
				p11 *=_screenToVisual ;
				positions.Add (new Point3D (p00.X, p00.Y, p00.Z)) ;
				positions.Add (new Point3D (p01.X, p01.Y, p01.Z)) ;
				positions.Add (new Point3D (p10.X, p10.Y, p10.Z)) ;
				positions.Add (new Point3D (p11.X, p11.Y, p11.Z)) ;
			}
			return (positions) ;
		}

		protected bool UpdateTransforms () {
			//_viewport =GetViewport3D (_model as Visual3D) ;
			//var newTransform =GetViewportTransform (_viewport) ;
			var newTransform =GetViewportTransform (_model) ;
			if ( double.IsNaN (newTransform.M11) )
				return (false) ;
			if ( !newTransform.HasInverse )
				return (false) ;
			if ( newTransform == _visualToScreen )
				return (false) ;
			_visualToScreen =newTransform ;
			_screenToVisual =newTransform ;
			_screenToVisual.Invert () ;
			if ( _viewport == null )
				_viewport =GetViewport3D (_model as Visual3D) ;
			_projectionToScreen =GetProjectionMatrix (_viewport) * GetViewportTransform (_viewport) ;
			Matrix3D inv =_projectionToScreen ;
			inv.Invert () ;
			_visualToProjection =_visualToScreen * inv ;
			return (true) ;
		}

		protected static Matrix3D GetProjectionMatrix (Viewport3D viewport) {
			return (GetProjectionMatrix (viewport.Camera, viewport.ActualHeight / viewport.ActualWidth)) ;
		}

		protected static Matrix3D GetViewportTransform (Viewport3D viewport) {
			return (new Matrix3D (
				viewport.ActualWidth / 2, 0, 0, 0,
				0, -viewport.ActualHeight / 2, 0, 0,
				0, 0, 1, 0,
				viewport.ActualWidth / 2, viewport.ActualHeight / 2, 0, 1
			)) ;
		}

		protected static Viewport3D GetViewport3D (Visual3D visual) {
			DependencyObject obj =visual ;
			while ( obj != null ) {
				var vis =obj as Viewport3DVisual ;
				if ( vis != null )
					return (VisualTreeHelper.GetParent (obj) as Viewport3D) ;
				obj =VisualTreeHelper.GetParent (obj) ;
			}
			return (null) ;
		}

		protected static Matrix3D GetViewportTransform (Visual3D visual) {
			var totalTransform =Matrix3D.Identity ;
			DependencyObject obj =visual ;
			while ( obj != null ) {
				var viewport3DVisual =obj as Viewport3DVisual ;
				if ( viewport3DVisual != null ) {
					var viewportTotalTransform =GetTotalTransform (viewport3DVisual) ;
					totalTransform.Append (viewportTotalTransform) ;
					return (totalTransform) ;
				}
				var mv =obj as ModelVisual3D ;
				if ( mv != null ) {
					if ( mv.Transform != null )
						totalTransform.Append (mv.Transform.Value) ;
				}
				obj =VisualTreeHelper.GetParent (obj) ;
			}
			throw new InvalidOperationException ("The visual is not added to a Viewport3D.") ;
			// At this point, we know obj is Viewport3DVisual
		}

		protected static Matrix3D GetTotalTransform (Viewport3DVisual viewport3DVisual) {
			var m =GetCameraTransform (viewport3DVisual) ;
			m.Append (GetViewportTransform (viewport3DVisual)) ;
			return (m) ;
		}

		protected static Matrix3D GetViewportTransform (Viewport3DVisual viewport3DVisual) {
			return (new Matrix3D (
				viewport3DVisual.Viewport.Width / 2, 0, 0, 0,
				0, -viewport3DVisual.Viewport.Height / 2, 0, 0,
				0, 0, 1, 0,
				viewport3DVisual.Viewport.X + (viewport3DVisual.Viewport.Width / 2),
				viewport3DVisual.Viewport.Y + (viewport3DVisual.Viewport.Height / 2),
				0, 1
			)) ;
		}

		protected static Matrix3D GetCameraTransform (Viewport3DVisual viewport3DVisual) {
			return (GetTotalTransform (viewport3DVisual.Camera, viewport3DVisual.Viewport.Size.Width / viewport3DVisual.Viewport.Size.Height)) ;
		}

		protected static Matrix3D GetTotalTransform (Camera camera, double aspectRatio) {
			var m =Matrix3D.Identity ;
			if ( camera.Transform != null ) {
				var cameraTransform = camera.Transform.Value ;
				if ( !cameraTransform.HasInverse )
					throw new Exception ("Camera transform has no inverse.") ;
				cameraTransform.Invert () ;
				m.Append (cameraTransform) ;
			}
			m.Append (GetViewMatrix (camera)) ;
			m.Append (GetProjectionMatrix (camera, aspectRatio)) ;
			return (m) ;
		}

		protected static Matrix3D GetViewMatrix (Camera camera) {
			if ( camera is MatrixCamera )
				return ((camera as MatrixCamera).ViewMatrix) ;
			if ( camera is ProjectionCamera ) {
				// Reflector on: ProjectionCamera.CreateViewMatrix
				var projectionCamera =camera as ProjectionCamera ;
				var zaxis =-projectionCamera.LookDirection ;
				zaxis.Normalize () ;
				var xaxis =Vector3D.CrossProduct (projectionCamera.UpDirection, zaxis) ;
				xaxis.Normalize () ;
				var yaxis =Vector3D.CrossProduct (zaxis, xaxis) ;
				var pos =(Vector3D)projectionCamera.Position ;
				return (new Matrix3D (
					xaxis.X, yaxis.X, zaxis.X, 0,
					xaxis.Y, yaxis.Y, zaxis.Y, 0,
					xaxis.Z, yaxis.Z, zaxis.Z, 0,
					-Vector3D.DotProduct (xaxis, pos), -Vector3D.DotProduct (yaxis, pos), -Vector3D.DotProduct (zaxis, pos), 1
				)) ;
			}
			throw new Exception ("Unknown camera type.") ;
		}

		protected static Matrix3D GetProjectionMatrix (Camera camera, double aspectRatio) {
			var perspectiveCamera =camera as PerspectiveCamera ;
			if ( perspectiveCamera != null ) {
				// The angle-to-radian formula is a little off because only
				// half the angle enters the calculation.
				double xscale =1 / Math.Tan (Math.PI * perspectiveCamera.FieldOfView / 360) ;
				double yscale =xscale * aspectRatio ;
				double znear =perspectiveCamera.NearPlaneDistance ;
				double zfar =perspectiveCamera.FarPlaneDistance ;
				double zscale =double.IsPositiveInfinity (zfar) ? -1 : (zfar / (znear - zfar)) ;
				double zoffset =znear * zscale ;
				return (new Matrix3D (xscale, 0, 0, 0, 0, yscale, 0, 0, 0, 0, zscale, -1, 0, 0, zoffset, 0)) ;
			}
			var orthographicCamera =camera as OrthographicCamera ;
			if ( orthographicCamera != null ) {
				double xscale =2.0 / orthographicCamera.Width ;
				double yscale =xscale * aspectRatio ;
				double znear =orthographicCamera.NearPlaneDistance ;
				double zfar =orthographicCamera.FarPlaneDistance ;
				if ( double.IsPositiveInfinity (zfar) )
					zfar = znear * 1e5 ;
				double dzinv =1.0 / (znear - zfar) ;
				var m =new Matrix3D (xscale, 0, 0, 0, 0, yscale, 0, 0, 0, 0, dzinv, 0, 0, 0, znear * dzinv, 1) ;
				return (m) ;
			}
			var matrixCamera =camera as MatrixCamera ;
			if ( matrixCamera != null )
				return (matrixCamera.ProjectionMatrix) ;
			throw new Exception ("Unknown camera type.") ;
		}
	
	}

}
