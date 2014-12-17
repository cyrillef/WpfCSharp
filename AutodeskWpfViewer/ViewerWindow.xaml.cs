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

using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;
using System.Globalization;

namespace Autodesk.ADN.Toolkit.Wpf.Viewer {

	enum DisplayMode { Wireframe, SmoothShade, WireframeOnShaded, Textured } ;

	public partial class ViewerWindow : Window {
		private DisplayMode _currentDisplayMode =DisplayMode.Textured ;
		private ModelVisual3D _currentMesh =null, _3dCubeMesh =null ;
		private Material _currentMeshMatGroup =null, _3dCubeMeshMatGroup =null ;
		private ModelVisual3D _currentWireframe =null ;
		private bool _wireframeDirty =true ;

		AdskTrackball _trackball ;
		AdskMayaOrbit _mayaOrbit ;
		Adsk3dNavigate _currentNavigation ;
		
		public ViewerWindow () {
			InitializeComponent () ;
			_trackball =new AdskTrackball (viewport, camera) ;
			_mayaOrbit =new AdskMayaOrbit (viewport, camera) ;
			_currentNavigation =_mayaOrbit ;
		}

		public void LoadModel (string location) {
			Load3dCube () ;
			FileStream zipStream =File.OpenRead (location) ;
			LoadModel (zipStream) ;
		}

		#region Utilities to create a 3D model out of OBJ meshes
		//protected void LoadTest () {
		//	//FileStream zipStream =File.OpenRead ("kRi3rCR0kqu14I7Logtp7J1fYEE.zip") ;
		//	//FileStream zipStream =File.OpenRead ("D9hVhzPKXNyKs1nmwdkF06PfWrg.zip") ;
		//	FileStream zipStream =File.OpenRead ("TAY0M381eVQyEsRhLXHdTQZRGdE.zip") ;
		//	LoadModel (zipStream) ;
		//}

		protected void LoadAUcubeExample () {
			// This is coming from our resources
			StreamResourceInfo stri =Application.GetResourceStream (new Uri (
				@"Examples\AUquads.zip",
				UriKind.Relative
			)) ;
			LoadModel (stri.Stream) ;
		}

		protected void Load3dCube () {
			// This is coming from our resources
			//StreamResourceInfo stri =Application.GetResourceStream (new Uri (
			//	@"Autodesk.ADN.Toolkit.Wpf.Viewer;3dCube\3dCube.zip",
			//	UriKind.Relative
			//)) ;
			//LoadModel (stri.Stream) ;
			Assembly assembly =Assembly.GetCallingAssembly () ;
			string resourceName =assembly.GetName ().Name + ".g" ;
			ResourceManager rm =new ResourceManager (resourceName, assembly) ;
			using ( ResourceSet set =rm.GetResourceSet (CultureInfo.CurrentCulture, true, true) ) {
				UnmanagedMemoryStream s =(UnmanagedMemoryStream)set.GetObject (@"3dCube/3dCube.zip", true) ;
				StreamResourceInfo stri =new StreamResourceInfo (s, "") ;
				using ( ZipArchive zip =new ZipArchive (stri.Stream) ) {
					ZipArchiveEntry mesh =zip.GetEntry ("mesh.obj") ;
					ZipArchiveEntry mtl =zip.GetEntry ("mesh.mtl") ;
					ZipArchiveEntry texture =zip.GetEntry ("tex_0.jpg") ;

					using ( new CursorSwitcher (null) ) {
						var objLoaderFactory =new ObjLoaderFactory () ;
						var objLoader =objLoaderFactory.Create (new ObjMaterialStreamProvider (mtl)) ;
						var result =objLoader.Load (mesh.Open ()) ;
						_3dCubeMesh =ObjTriangleMeshAdapater.BuildVisualModel (result, texture) ;
						_3dCubeMeshMatGroup =(_3dCubeMesh.Content as GeometryModel3D).Material ;
						// Reset the model & transform(s)
						this.cubeModel.Children.Clear () ;
						this.cubeModel.Children.Add (_3dCubeMesh) ;
					}
				}
			}
		}

		protected void LoadModel (Stream str) {
			using ( ZipArchive zip =new ZipArchive (str) ) {
				ZipArchiveEntry mesh =zip.GetEntry ("mesh.obj") ;
				ZipArchiveEntry mtl =zip.GetEntry ("mesh.mtl") ;
				ZipArchiveEntry texture =zip.GetEntry ("tex_0.jpg") ;

				using ( new CursorSwitcher (null) ) {
					var objLoaderFactory =new ObjLoaderFactory () ;
					var objLoader =objLoaderFactory.Create (new ObjMaterialStreamProvider (mtl)) ;
					var result =objLoader.Load (mesh.Open ()) ;
					_currentWireframe =null ;
					_currentMesh =ObjTriangleMeshAdapater.BuildVisualModel (result, texture) ;
					_currentMeshMatGroup =(_currentMesh.Content as GeometryModel3D).Material ;
					// Reset the model & transform(s)
					this.model.Children.Clear () ;
					_currentDisplayMode =DisplayMode.Textured ;
					this.model.Children.Add (_currentMesh) ;
				}
			}
			Home_Click (null, null) ;
		}

		protected void BuildWireframeModel (double depthOffset) {
			if ( _currentWireframe != null && _wireframeDirty == false )
				return ;
			_wireframeDirty =false ;
			bool isPresent =model.Children.Contains (_currentMesh) ;
			if ( !isPresent )
				model.Children.Add (_currentMesh) ;
			_currentWireframe =WireframeMeshAdapter.BuildVisualModel (_currentMesh, 1.0, depthOffset) ;
			if ( !isPresent )
				model.Children.Remove (_currentMesh) ;
		}

		#endregion

		#region Controlling the 3D view camera
		private void Grid_MouseDown (object sender, MouseButtonEventArgs e) {
			Mouse.Capture (canvas, CaptureMode.Element) ;
			_currentNavigation.Position =Mouse.GetPosition (viewport) ; //e.GetPosition ()
		}

		private void Grid_MouseMove (object sender, MouseEventArgs e) {
			Point pos =Mouse.GetPosition (viewport) ;
			if ( e.LeftButton == MouseButtonState.Pressed )
				Viewport_Rotate (pos) ;
            else if (e.MiddleButton == MouseButtonState.Pressed || (e.RightButton == MouseButtonState.Pressed && (Keyboard.Modifiers & ModifierKeys.Alt) > 0) )
				Viewport_Pan (pos) ;
		}

		private void Grid_MouseUp (object sender, MouseButtonEventArgs e) {
			Mouse.Capture (canvas, CaptureMode.None) ;
			RebuildWireFrame () ;
		}

		private void Viewport_Pan (Point actualPos) {
			Vector3D tr =_currentNavigation.Viewport_Pan (actualPos) ;
			foreach ( Visual3D child in model.Children ) {
				Transform3DGroup transformGroup =child.Transform as Transform3DGroup ;
				transformGroup.Children.Add (new TranslateTransform3D (tr)) ; // Remember we are up=<0,-1,0>
			}

			_wireframeDirty =true ;
		}

		private void Viewport_Rotate (Point actualPos) {
			Quaternion quat =_currentNavigation.Viewport_Rotate (actualPos) ;
			QuaternionRotation3D r =new QuaternionRotation3D (quat) ;
			foreach ( Visual3D child in model.Children ) {
				Transform3DGroup transformGroup =child.Transform as Transform3DGroup ;
				transformGroup.Children.Add (new RotateTransform3D (r)) ;
			}
			foreach ( Visual3D child in cubeModel.Children ) {
				Transform3DGroup transformGroup =child.Transform as Transform3DGroup ;
				transformGroup.Children.Add (new RotateTransform3D (r)) ;
			}

			_wireframeDirty =true ;
		}

		private void Grid_MouseWheel (object sender, MouseWheelEventArgs e) {
			e.Handled =true ;

			Vector3D lookAt =camera.LookDirection ;
			//lookAt.Negate () ;
			lookAt.Normalize () ;
			lookAt *=e.Delta / 250.0d ;
			Transform3DGroup transformGroup =camera.Transform as Transform3DGroup ;
			transformGroup.Children.Add (new TranslateTransform3D (lookAt)) ;

			_wireframeDirty =true ;
			RebuildWireFrame () ;
		}

		private void RebuildWireFrame () {
			if ( _wireframeDirty == false || (_currentDisplayMode != DisplayMode.Wireframe && _currentDisplayMode != DisplayMode.WireframeOnShaded ) )
				return ;
			model.Children.Remove (_currentWireframe) ;
			BuildWireframeModel (0.0) ; //_currentDisplayMode == DisplayMode.Wireframe ? 0.0 : 1.0) ;
			model.Children.Add (_currentWireframe) ;

			foreach ( Visual3D child in model.Children )
				CleanChildTransforms (child) ;
		}

		private void CleanChildTransforms (Visual3D child) {
			Transform3DGroup transformGroup =child.Transform as Transform3DGroup ;
			for ( int i =(child == _currentMesh || child == _currentWireframe ? 2 : 0) ; i < transformGroup.Children.Count ; /*i++*/ )
				transformGroup.Children.RemoveAt (i) ;
			if ( _currentNavigation.Translation.Length != 0 )
				transformGroup.Children.Add (new TranslateTransform3D (_currentNavigation.Translation)) ;
			if ( !_currentNavigation.Rotation.IsIdentity )
				transformGroup.Children.Add (new RotateTransform3D (new QuaternionRotation3D (_currentNavigation.Rotation))) ;
		}

		#endregion

		#region Viewport Options
		private void Home_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;

			//_viewportT =new Vector3D () ;
			//_viewportR =new Quaternion () ;
			_trackball.Clear () ;
			_mayaOrbit.Clear () ;

			// 0 and 1 are origin scale and translate
			foreach ( Visual3D child in model.Children )
				CleanChildTransforms (child) ;
			foreach ( Visual3D child in cubeModel.Children )
				CleanChildTransforms (child) ;
	
			Transform3DGroup transformGroup =camera.Transform as Transform3DGroup ;
			transformGroup.Children.Clear () ;
		}

		private void Wireframe_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;

			if ( _currentDisplayMode == DisplayMode.Wireframe )
				return ;
			model.Children.Remove (_currentWireframe) ;
			BuildWireframeModel (0.0) ;
			if ( !model.Children.Contains (_currentWireframe) )
				model.Children.Add (_currentWireframe) ;
			model.Children.Remove (_currentMesh) ;
			_currentDisplayMode =DisplayMode.Wireframe ;

			foreach ( Visual3D child in model.Children )
				CleanChildTransforms (child) ;
		}

		private void SmoothShade_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;

			if ( _currentDisplayMode == DisplayMode.SmoothShade )
				return ;
			if ( !model.Children.Contains (_currentMesh) )
				model.Children.Add (_currentMesh) ;
			model.Children.Remove (_currentWireframe) ;
			_currentDisplayMode =DisplayMode.SmoothShade ;
			(_currentMesh.Content as GeometryModel3D).Material =(_currentMesh.Content as GeometryModel3D).BackMaterial ;

			foreach ( Visual3D child in model.Children )
				CleanChildTransforms (child) ;
		}

		private void WireframeOnShaded_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;

			if ( _currentDisplayMode == DisplayMode.WireframeOnShaded )
				return ;
			if ( !model.Children.Contains (_currentMesh) )
				model.Children.Add (_currentMesh) ;
			BuildWireframeModel (0.0/*1.0*/) ;
			if ( !model.Children.Contains (_currentWireframe) )
				model.Children.Add (_currentWireframe) ;
			_currentDisplayMode =DisplayMode.WireframeOnShaded ;
			(_currentMesh.Content as GeometryModel3D).Material =(_currentMesh.Content as GeometryModel3D).BackMaterial ;

			foreach ( Visual3D child in model.Children )
				CleanChildTransforms (child) ;
		}

		private void Textured_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;

			if ( _currentDisplayMode == DisplayMode.Textured )
				return ;
			model.Children.Remove (_currentWireframe) ;
			if ( !model.Children.Contains (_currentMesh) ) 
				model.Children.Add (_currentMesh);
			_currentDisplayMode =DisplayMode.Textured ;
			(_currentMesh.Content as GeometryModel3D).Material =_currentMeshMatGroup ;

			foreach ( Visual3D child in model.Children )
				CleanChildTransforms (child) ;
		}

		private void trackballToggle_Checked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			_currentNavigation =_trackball ;
		}

		private void trackballToggle_Unchecked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			_currentNavigation =_mayaOrbit ;
		}

		private void ambiantlightToggle_Checked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( !lights.Children.Contains (ambientLightMain) )
				lights.Children.Add (ambientLightMain) ;
		}

		private void ambiantlightToggle_Unchecked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			lights.Children.Remove (ambientLightMain) ;
		}

		private void dirlightToggle_Checked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( !lights.Children.Contains (dirLightMain) )
				lights.Children.Add (dirLightMain) ;
		}

		private void dirlightToggle_Unchecked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			lights.Children.Remove (dirLightMain) ;
		}

		#endregion

	}

}