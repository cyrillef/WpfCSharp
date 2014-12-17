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
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;

namespace Autodesk.ADN.Toolkit.Wpf.Viewer {

	public class ObjMaterialStreamProvider : IMaterialStreamProvider {
		public ZipArchiveEntry _mtl ;

		public ObjMaterialStreamProvider (ZipArchiveEntry mtl) {
			_mtl =mtl ;
		}

		public Stream Open (string materialFilePath) {
			return (_mtl.Open ()) ;
		}

	}
	
	// Utility Class for converting data containing a Maya MFnMesh into an object that is compatible
	// with the Windows Presentation framework. 
	public class ObjTriangleMeshAdapater {
		protected Point3DCollection Points ;
		protected Vector3DCollection Normals ;
		protected Int32Collection Indices ;
		protected PointCollection TexCoords ;

		protected ObjTriangleMeshAdapater () {}

		public static ModelVisual3D BuildVisualModel (LoadResult objmesh, ZipArchiveEntry texture) {
			ObjTriangleMeshAdapater adapter =new ObjTriangleMeshAdapater () ;
			return (adapter.MakeVisualModel (objmesh, texture)) ;
		}

		protected void TriangleMeshAdapater (LoadResult objmesh) {
			HashSet<string> uniqPairs =new HashSet<string> () ;
			List<Face> newFaces =new List<Face> () ;
			foreach ( Face face in objmesh.Groups [0].Faces ) {
				// Create vertex/tex pairs list
				for ( int i =0 ; i < face.Count ; i++ ) {
					FaceVertex fv =face [i] ;
					string pairName =string.Format ("{0}/{1}", fv.VertexIndex, fv.TextureIndex < 0 ? 0 : fv.TextureIndex) ;
					uniqPairs.Add (pairName) ;
				}
				// Split quads into triangles
				if ( face.Count == 4 ) { // a quad
					//throw new NotImplementedException ("Face needs to be triangulated!"); 
					Face glface =new Face () ;
					glface.AddVertex (new FaceVertex (face [0].VertexIndex, face [0].TextureIndex, face [0].NormalIndex)) ;
					glface.AddVertex (new FaceVertex (face [2].VertexIndex, face [2].TextureIndex, face [2].NormalIndex)) ;
					glface.AddVertex (new FaceVertex (face [3].VertexIndex, face [3].TextureIndex, face [3].NormalIndex)) ;
					// Added the following in Face.cs
					//public void RemoveVertexAt (int index) { _vertices.RemoveAt (index); }
					face.RemoveVertexAt (3) ;
					newFaces.Add (glface) ;
				} else if ( face.Count > 4 ) {
					throw new NotImplementedException ("Face needs to be triangulated!") ;
				}
			}
			((List<Face>)(objmesh.Groups [0].Faces)).AddRange (newFaces) ;
			
			// Build OpenGL vertex / tex arrrays
			int nbPairs =uniqPairs.Count ;
			string [] pairs =new string [nbPairs] ;
			uniqPairs.CopyTo (pairs) ;
			Points =new Point3DCollection (nbPairs) ;
			TexCoords =new PointCollection (nbPairs) ;
			foreach ( string pairName in pairs ) {
				string [] def =pairName.Split ('/') ;
				ObjLoader.Loader.Data.VertexData.Vertex vertex =objmesh.Vertices [Convert.ToInt32 (def [0]) - 1] ;
				Points.Add (new Point3D (vertex.X, vertex.Y, vertex.Z)) ;
				ObjLoader.Loader.Data.VertexData.Texture t =objmesh.Textures [Convert.ToInt32 (def [1]) == 0 ? 0 : Convert.ToInt32 (def [1]) - 1] ;
				TexCoords.Add (new System.Windows.Point (t.X, 1.0 - t.Y)) ;
				//System.Diagnostics.Debug.Print ("{0}\t- {1},\t{2},\t{3}\t- {4}\t{5}", Points.Count, vertex.X, vertex.Y, vertex.Z, t.X, t.Y) ;
			}
			//System.Diagnostics.Debug.Print (" ") ;

			Normals =new Vector3DCollection () ;
			Indices =new Int32Collection () ;
			foreach ( Face face in objmesh.Groups [0].Faces ) {
				for ( int i =0 ; i < face.Count ; i++ ) {
					FaceVertex fv =face [i] ;
					string pairName =string.Format ("{0}/{1}", fv.VertexIndex, fv.TextureIndex < 0 ? 0 : fv.TextureIndex) ;
					int index =Array.IndexOf (pairs, pairName) ;
					Indices.Add (index) ;
					//System.Diagnostics.Debug.Print ("{0}\t/{1}\t= {2}", i, pairName, index) ;
				}
			}
		}

		protected MeshGeometry3D MakeGeometry (LoadResult objmesh) {
			var r =new MeshGeometry3D () ;
			TriangleMeshAdapater (objmesh) ;
			r.Positions =Points ;
			r.TriangleIndices =Indices ;
			r.Normals =Normals ;
			r.TextureCoordinates =TexCoords ;
			return (r) ;
		}

		// MTL explanation: http://www.kixor.net/dev/objloader/
		protected Material MakeMaterial (LoadResult objmesh, ZipArchiveEntry texture) {
			MaterialGroup matGroup =new MaterialGroup () ;

			ObjLoader.Loader.Data.Material material =objmesh.Materials [0] ;

			Stream imgStream =texture.Open () ;
			Byte [] buffer =new Byte [texture.Length] ;
			imgStream.Read (buffer, 0, buffer.Length) ;
			var byteStream =new System.IO.MemoryStream (buffer) ;

			//ImageBrush imgBrush =new ImageBrush (new BitmapImage (new Uri (@"C:\Users\cyrille\Documents\Visual Studio 2012\Projects\tex_0.jpg"))) ;
			BitmapImage bitmap =new BitmapImage () ;
			bitmap.BeginInit () ;
			bitmap.CacheOption =BitmapCacheOption.OnLoad ;
			bitmap.StreamSource =byteStream ;
			bitmap.EndInit () ;
			ImageBrush imgBrush =new ImageBrush (bitmap) ;
			imgBrush.ViewportUnits =BrushMappingMode.Absolute ;
			//imgBrush.ViewportUnits =BrushMappingMode.RelativeToBoundingBox ;

			//Brush brush =new SolidColorBrush (Color.FromScRgb (material.Transparency, material.DiffuseColor.X, material.DiffuseColor.Y, material.DiffuseColor.Z)) ;
			//brush.Opacity =material.Transparency ;

			DiffuseMaterial diffuse =new DiffuseMaterial (imgBrush) ;
			diffuse.AmbientColor =Color.FromScRgb (material.Transparency, material.AmbientColor.X, material.AmbientColor.Y, material.AmbientColor.Z) ;
			// no more attributes
			matGroup.Children.Add (diffuse) ;

			SpecularMaterial specular =new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (material.Transparency, material.SpecularColor.X, material.SpecularColor.Y, material.SpecularColor.Z)), material.SpecularCoefficient) ;
			// no more attributes
			matGroup.Children.Add (specular) ;

			// Default to Blue
			if ( matGroup.Children.Count == 0 )
				matGroup.Children.Add (new DiffuseMaterial (new SolidColorBrush (Color.FromRgb (0, 0, 255)))) ;
			return (matGroup) ;
		}

		protected Material MakeBackMaterial () {
			MaterialGroup matGroup =new MaterialGroup () ;
			// Default to Maya Gray
			matGroup.Children.Add (new DiffuseMaterial (new SolidColorBrush (Color.FromRgb (70, 70, 70)))) ;
			return (matGroup) ;
		}

		protected Model3D MakeModel (LoadResult objmesh, ZipArchiveEntry texture) {
			GeometryModel3D ret =new GeometryModel3D (MakeGeometry (objmesh), MakeMaterial (objmesh, texture)) ;
			ret.BackMaterial =MakeBackMaterial () ;
			return (ret) ;
		}

		protected ModelVisual3D MakeVisualModel (LoadResult objmesh, ZipArchiveEntry texture) {
			var r =new ModelVisual3D () ;
			r.Content =MakeModel (objmesh, texture) ;
			Points.Freeze () ; // We won't change them anymore
			Normals.Freeze () ;
			Indices.Freeze () ;
			TexCoords.Freeze () ;

			r.Transform =new Transform3DGroup () ;
			Transform3DGroup transformGroup =r.Transform as Transform3DGroup ;
			TranslateTransform3D translation =new TranslateTransform3D (
				-(r.Content.Bounds.X + r.Content.Bounds.SizeX / 2),
				-(r.Content.Bounds.Y + r.Content.Bounds.SizeY / 2),
				-(r.Content.Bounds.Z + r.Content.Bounds.SizeZ / 2)
			) ;
			transformGroup.Children.Add (translation) ;

			double scale =Math.Abs (1 / (r.Content.Bounds.SizeX)) ;
			scale =Math.Min (scale, Math.Abs (1 / (r.Content.Bounds.SizeY))) ;
			scale =Math.Min (scale, Math.Abs (1 / (r.Content.Bounds.SizeZ))) ;
			ScaleTransform3D scaletr =new ScaleTransform3D (scale, scale, scale) ;
			transformGroup.Children.Add (scaletr) ;

			return (r) ;
		}

		//public TriangleMeshAdapater (LoadResult objmesh) {
		//	Points =new Point3DCollection (objmesh.Vertices.Count) ;
		//	foreach ( ObjLoader.Loader.Data.VertexData.Vertex vertex in objmesh.Vertices )
		//		Points.Add (new Point3D (vertex.X, vertex.Y, vertex.Z)) ;

		//	Normals =new Vector3DCollection () ;

		//	Indices =new Int32Collection () ;
		//	foreach ( Face face in objmesh.Groups [0].Faces ) {
		//		if ( face.Count != 3 )
		//			throw new NotImplementedException ("Only triangles are supported");
		//		for ( int i =0 ; i < face.Count ; i++ ) {
		//			var faceVertex =face [i] ;
		//			Indices.Add (faceVertex.VertexIndex - 1) ;
		//		}
		//	}

		//	TexCoords =new PointCollection (Indices.Count) ;
		//	foreach ( ObjLoader.Loader.Data.VertexData.Texture t in objmesh.Textures )
		//		TexCoords.Add (new System.Windows.Point (t.X, t.Y)) ;
		//}

	}

}
