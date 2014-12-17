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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Autodesk.ADN.Toolkit.ReCap {

	public class AdskReCapPhotoscene {

		#region Properties
		[Category("Photoscene")]
		[DisplayName("Photoscene ID")]
		[Description("Internal database ID for the Photoscene")]
		public string PhotoSceneID { get; private set; }
		[Category("Photoscene")]
		[DisplayName("Scene Name")]
		[Description("Name of the scene as it appears in the final filename")]
		public string Name { get; private set; }
		[Category("Photoscene")]
		[DisplayName("Creation Date")]
		[Description("Date/Time when the scene was first created")]
		public DateTime CreationDate { get; private set; }
		[Category("Mesh Data")]
		[DisplayName("Mesh Quality")]
		[Description("Requested quality for the generated mesh")]
		[DefaultValueAttribute(AdskReCap.MeshQuality.DRAFT)]
		public AdskReCap.MeshQuality MeshQuality { get; private set; }
		[Category("Misc")]
		[DisplayName("Status")]
		public string Status { get; private set; }
		[Category("Photoscene")]
		[DisplayName("Convert Format")]
		[DefaultValueAttribute (AdskReCap.Format._3DP)]
		public AdskReCap.Format ConvertFormat { get; private set; }
		[Category("Misc")]
		[DisplayName("Convert Status")]
		public AdskReCap.WorkerStatus ConvertStatus { get; private set; }
		[Category("Mesh Data")]
		[DisplayName ("Faces")]
		[Description("Number of faces in the generated mesh")]
		public int NbFaces { get; private set; }
		[Category("Mesh Data")]
		[DisplayName("Vertices")]
		[Description("Number of vertices in the generated mesh")]
		public int NbVertices { get; private set; }
		[Category("Mesh Data")]
		[DisplayName("3d Points")]
		[Description("Number of 3D points in the generated mesh")]
		public int Nb3dPoints { get; private set; }
		[Category("Photogrammetry")]
		[DisplayName("Shots")]
		[Description("Number of images loaded in the Photoscene")]
		public int NbShots { get; private set; }
		[Category("Photogrammetry")]
		[DisplayName("Stitched Shots")]
		[Description("Number of images used to extract the generated mesh")]
		public int NbStitchedShots { get; private set; }
		[Category("Photoscene")]
		[DisplayName("Deleted")]
		[Description("If true, the Photoscene and its resources were deleted from the server")]
		[DefaultValueAttribute(false)]
		public bool Deleted { get; private set; }
		[Category("Photoscene")]
		[DisplayName("File Size")]
		[Description("Size on disc for all documents used to or created by the Photoscene")]
		public int FileSize { get; private set; }
		[Category("Photogrammetry")]
		[DisplayName("Files")]
		public List<string> Files { get; private set; }
		[Category("Misc")]
		[DisplayName("Processing Time")]
		public double ProcessingTime { get; private set; }
		[Category("Misc")]
		[DisplayName("Progress")]
		[Description("Current progress of a Photoscene (in %)")]
		public double Progress { get; private set; }
		[Category("Misc")]
		[DisplayName("Progress Message")]
		public string ProgressMessage { get; private set; }
		[Category("Mesh Data")]
		[DisplayName("Scene Link")]
		[Description("Link to download the processed Photoscene")]
		public Uri SceneLink { get; private set; }
		[Category("Misc")]
		[DisplayName("User ID")]
		[Description("Internal database User ID of the person who created the Photoscene")]
		public string UserID { get; private set; }

		#endregion

		#region Constructors
		// XML Serializer - http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlserializer.aspx
		public AdskReCapPhotoscene (XmlDocument doc)
			: this (doc.SelectSingleNode ("/Response/Photoscenes/Photoscene")) {
		}

		public AdskReCapPhotoscene (XmlNode el) {
			try {
				XmlNode node =el.SelectSingleNode ("./photosceneid") ;
				PhotoSceneID =node.InnerText ;
				node =el.SelectSingleNode ("./name") ;
				Name =node.InnerText ;
				node =el.SelectSingleNode ("./creationDate") ;
				CreationDate =DateTime.Parse (Uri.UnescapeDataString (node.InnerText)) ;
				node =el.SelectSingleNode ("./meshQuality") ;
				MeshQuality =(AdskReCap.MeshQuality)Enum.Parse (typeof (AdskReCap.MeshQuality), node.InnerText, true) ;
				node =el.SelectSingleNode ("./status") ;
				Status =node.InnerText ;
				node =el.SelectSingleNode ("./convertFormat") ;
				ConvertFormat =(AdskReCap.Format)node.InnerText.ToReCapFormatEnum () ;
				node =el.SelectSingleNode ("./convertStatus") ;
				ConvertStatus =(AdskReCap.WorkerStatus)Enum.Parse (typeof (AdskReCap.WorkerStatus), node.InnerText, true) ;
				try {
					node =el.SelectSingleNode ("./nbfaces") ;
					NbFaces =int.Parse (node.InnerText) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./nbvertices") ;
					NbVertices =int.Parse (node.InnerText) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./nb3Dpoints") ;
					Nb3dPoints =int.Parse (node.InnerText) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./nbStitchedShots") ;
					NbStitchedShots =int.Parse (node.InnerText) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./nbShots") ;
					NbShots =int.Parse (node.InnerText) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./fileSize") ;
					FileSize =(node != null ? int.Parse (node.InnerText) : -1) ;
				} catch { }
				//node =el.SelectSingleNode ("./nbShots") ;
				//public List<string> Files { get; private set; }
				try {
					node =el.SelectSingleNode ("./processingTime") ;
					ProcessingTime =double.Parse (node.InnerText) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./progress") ;
					Progress =(node != null ? double.Parse (node.InnerText) : 0) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./progressMessage") ;
					ProgressMessage =(node != null ? node.InnerText : "") ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./scenelink") ;
					SceneLink =(node != null ? new Uri ((string)node.InnerText) : null) ;
				} catch { }
				try {
					node =el.SelectSingleNode ("./deleted") ;
					Deleted =(node != null ? bool.Parse (node.InnerText) : false) ;
				} catch { }
				node =el.SelectSingleNode ("./userID") ;
				UserID =Uri.UnescapeDataString (node.InnerText) ;
			} catch {
			}
		}

		// LINQ to XML - http://msdn.microsoft.com/en-us/library/bb308960.aspx
		// http://www.codeproject.com/Tips/366993/Convert-XML-to-Object-using-LINQ
		public AdskReCapPhotoscene (XDocument doc)
			: this (doc.Element ("Response").Element ("Photoscenes").Elements ("Photoscene").First ()) {
		}

		public AdskReCapPhotoscene (XElement el) {
			try {
				PhotoSceneID =(string)el.Element ("photosceneid") ;
				Name =(string)el.Element ("name") ;
				CreationDate =DateTime.Parse (Uri.UnescapeDataString (el.Element ("creationDate").Value)) ;
				MeshQuality =(AdskReCap.MeshQuality)Enum.Parse (typeof (AdskReCap.MeshQuality), el.Element ("meshQuality").Value, true) ;
				Status =(string)el.Element ("status") ;
				ConvertFormat =(AdskReCap.Format)(el.Element ("convertFormat").Value.ToReCapFormatEnum ()) ;
				ConvertStatus =(AdskReCap.WorkerStatus)Enum.Parse (typeof (AdskReCap.WorkerStatus), el.Element ("convertStatus").Value, true) ;
				try { NbFaces =(int)el.Element("nbfaces") ; } catch { }
				try { NbVertices =(int)el.Element ("nbvertices") ; } catch { }
				try { Nb3dPoints =(int)el.Element ("nb3Dpoints") ; } catch { }
				try { NbStitchedShots =(int)el.Element ("nbStitchedShots") ; } catch { }
				try { NbShots =(int)el.Element ("nbShots") ; } catch { }
				try { Deleted =(el.Element ("deleted") != null ? (bool)el.Element ("deleted") : false) ; } catch { }
				try { FileSize =(el.Element ("fileSize") != null ? (int)el.Element ("fileSize") : -1) ; } catch { }
				//public List<string> Files { get; private set; }
				try { ProcessingTime =(double)el.Element ("processingTime") ; } catch { }
				try { Progress =(el.Element ("progress") != null ? (double)el.Element ("progress") : 0) ; } catch { }
				try { ProgressMessage =(el.Element ("progressMessage") != null ? (string)el.Element ("progressMessage") : "") ; } catch { }
				try { SceneLink =(el.Element ("scenelink") != null ? new Uri ((string)el.Element ("scenelink")) : null) ; } catch { }
				UserID =Uri.UnescapeDataString ((string)el.Element ("userID")) ;
			} catch {
			}
		}

		// http://james.newtonking.com/json/help/index.html#
		public AdskReCapPhotoscene (JObject el) {
			try {
				if ( el ["Photoscenes"] != null )
					el =(JObject)el ["Photoscenes"] ["Photoscene"] ;

				PhotoSceneID =(string)el ["photosceneid"] ;
				Name =(string)el ["name"] ;
				CreationDate =DateTime.Parse (Uri.UnescapeDataString ((string)el ["creationDate"])) ;
				MeshQuality =(AdskReCap.MeshQuality)Enum.Parse (typeof (AdskReCap.MeshQuality), (string)el ["meshQuality"], true) ;
				Status =(string)el ["status"] ;
				ConvertFormat =(AdskReCap.Format)((string)el ["convertFormat"]).ToReCapFormatEnum () ;
				ConvertStatus =(AdskReCap.WorkerStatus)Enum.Parse (typeof (AdskReCap.WorkerStatus), (string)el ["convertStatus"], true) ;
				NbFaces =(int)el ["nbfaces"] ;
				NbVertices =(int)el ["nbvertices"] ;
				Nb3dPoints =(int)el ["nb3Dpoints"] ;
				NbStitchedShots =(int)el ["nbStitchedShots"] ;
				NbShots =(int)el ["nbShots"] ;
				Deleted =(el ["deleted"] != null ? (bool)el ["deleted"] : false) ;
				FileSize =(el ["fileSize"] != null ? (int)el ["fileSize"] : -1) ;
				//public List<string> Files { get; private set; }
				ProcessingTime =(double)el ["processingTime"] ;
				Progress =(el ["progress"] != null ? (double)el ["progress"] : 0) ;
				ProgressMessage =(el ["progressMessage"] != null ? (string)el ["progressMessage"] : "") ;
				SceneLink =(el ["scenelink"] != null ? new Uri ((string)el ["scenelink"]) : null) ;
				UserID =Uri.UnescapeDataString ((string)el ["userID"]) ;
			} catch {
			}
		}

		protected AdskReCapPhotoscene () {
		}

		#endregion

	}

}
