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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Autodesk.ADN.Toolkit.RESTful;

namespace Autodesk.ADN.Toolkit.ReCap {

	// The AdskRESTful _adskRESTClient is a temporary workaround while there is a RESTful
	// client incompatibilty in the DeleteScene API for ReCap.

	// In older release of the API, you needed to have, but this is not required anymore
	//   request.AddParameter ("timestamp", AdskRESTful.timestamp ()) ;

	public static class EnumExtensions {

		public static string ToFriendlyString (this Enum en) {
			Type type =en.GetType() ;
			if ( type == typeof (AdskReCap.Format) )
				return (en.ToString ().TrimStart (new char [] { '_' })) ;
			return (en.ToString ()) ;
		}

		public static Enum ToReCapFormatEnum (this string formatSt) {
			AdskReCap.Format format =AdskReCap.Format.OBJ ;
			try {
				format =(AdskReCap.Format)Enum.Parse (typeof (AdskReCap.Format), formatSt, true) ;
			} catch {
				format =(AdskReCap.Format)Enum.Parse (typeof (AdskReCap.Format), "_" + formatSt, true) ;
			}
			return (format) ;
		}

		public static Enum ToEnum<T> (this string st) {
			return ((Enum)Enum.Parse (typeof (T), st, true)) ;
		}

		public static bool IsEnum<T> (this string st) {
			return (Enum.IsDefined (typeof (T), st)) ;
		}

		public static T? ToEnumSafe<T> (this string st) where T : struct {
			return (IsEnum<T> (st) ? (T?)Enum.Parse (typeof (T), st, true) : null) ;
		}

	}


	public class AdskReCap {
		#region Enums
		public enum NotificationType { ERROR, DONE } ;
		public enum Format { _3DP, RCP, FBX, IPM, LAS, OBJ, FYSC, RCS, RCM } ;
		[Description("'WorkerStatus' is a common field, that could be used as a parameter or can be a returned value. 'WorkerStatus' is the corresponding state of the photoscene.")]
		public enum WorkerStatus {
			[Description("the photoscene has just been created")]
			CREATED,
			[Description("image(s) had been uploaded to this photoscene")]
			SENT,
			[Description("the photoscene is finished. Client can get the result file.")]
			DONE,
			[Description("client had canceled the photoscene. If the photoscene has not already been processed, it will never be.")]
			CANCEL_BY_USER,
			[Description("the photoscene is currently processing")]
			PROCESSING,
			[Description("the photoscene is currently processing")]
			PROCESSING_ALL,
			[Description("the photoscene is currently processing")]
			RUNNING_KP,
			[Description("an error occured while processing the photoscene.")]
			ERROR
		} ;
		[Description("Error Code returned from the API")]
		public enum Error {
			[Description("No error")] NO_ERROR =0,
			[Description("General error")] ERROR =1,
			[Description("Database error")] DB_ERROR =2,
			[Description("Given ID doesn't match one in database")] DB_BAD_ID =3,
			[Description("Not yet implemented")] NOT_YET =4,
			[Description("The given type is not valid")] BAD_TYPE =5,
			[Description("The resource doesn't have an id")] EMPTY_RESOURCE_ID =6,
			[Description("The resource has an id but shouldn't")] NOT_EMPTY_RESOURCE_ID =7,
			[Description("The used resource is not correct")] BAD_RESOURCE =8,
			[Description("You need at least 3 images to process a photoscene")] NOT_ENOUGH_IMAGES =9,
			[Description("Given attribute already exists")] DB_ALREADY_EXISTS =10,
			[Description("Unknown")] UNKNOWN =11,
			[Description("Bad authentication")] BAD_AUTHENTICATION =12,
			[Description("Current user cannot access requested data")] SECURITY_ERROR =13,
			[Description("Given values are not correct")] BAD_VALUES =14,
			[Description("Given client doesn't exist or is invalid")] CLIENT_DOESNT_EXIST =15,
			[Description("Bad timestamp")] BAD_TIMESTAMP =16,
			[Description("Given FileID doesn t exist")] FILE_DOESNT_EXIST =17,
			[Description("The given image protocol is not correct")] BAD_IMAGE_PROTOCOL =18,
			[Description("The given Photoscene ID doesn t exist in the database")] BAD_SCENE_ID =19,
			[Description("The user is not correctly identified")] USER_NOT_IDENTIFIED =20,
			[Description("You don't have the credentials to use this function")] NO_CREDENTIALS =21,
			[Description("Your data is not ready")] NOT_READY =22,
			[Description("One file of the same kind already exists in the repository, you cannot overwrite it")] FILE_ALREADY_EXISTS =23,
			[Description("This photoscene has already been processed you cannot change the source file you must create a new Photoscene with this photoscene as reference")] SCENE_ALREADY_PROCESSED =24,
			[Description("You don't have currently the correct rights")] NO_RIGHTS =25,
			[Description("Processing message cannot be sent")] CANNOT_SEND_MESSAGE =26,
			[Description("This client is not valid. Please contact ReCap.Api (at) autodesk.com")] CLIENT_NOT_ACTIVATED =27,
			[Description("The scene name cannot be empty")] SCENE_NAME_EMPTY =28,
			[Description("This client cannot access the asked resource")] PERMISSION_DENIED =29,
			[Description("The reference photoscene ID is missing")] MISSING_REF_PID =30,
			[Description("Email address has not been entered for this user")] NO_EMAIL =31,
			[Description("Message doesn't exist")] ERROR_MSG_DOESNT_EXIST =32,
			[Description("An error occured while sending the notification")] ERROR_SENDING_NOTIFICATION =33,
			[Description("An error occured while copying the file")] CANT_COPY_FILE =34,
			[Description("Photoscene seems to have corrupted information (parameters, files) ...")] PHOTOSCENE_CORRUPTED =35,
			[Description("The given notification callback protocol is not correct")] BAD_NOTIFICATION_PROTOCOL =36,
			[Description("No callback has been defined")] NO_CALLBACK_DEFINED =37,
			[Description("Given user doesn't exist or is invalid")] USER_DOESNT_EXIST =38,
			[Description("The service was unable to create new photoscene")] CANNOT_ALLOCATE_PHOTOSCENEID =39,
			[Description("Given reference project ID doesn't match one in database")] BAD_REFERENCE_PROJECT_ID =40,
			[Description("The source file is unreachable.")] CANT_RETRIEVE_PHOTOSCENE_FILE =41,
			[Description("Source file seems to be corrupted. Your source file was probably saved in UTF-8 instead of UTF-16 (Unicode)")] CANT_READ_PHOTOSCENE_FILE =42,
			[Description("The namespace associated to client is not found.")] NAMESPACE_NOT_FOUND =43,
			[Description("Bad O2 Authentication (signature)")] BAD_O2_AUTHENTICATION =44,
			[Description("No OAuth header has been found")] OAUTH_HEADER_DOESNT_FIND =45,
			[Description("The specified project is not finished")] PROJECT_NOT_FINISHED =46,
		} ;
		[Description ("Error Code returned while getting Photoscene link or properties. These errors means the computation of the photoscene failed. (but the API request successed)")]
		public enum ComputationError {
			[Description("Photoscene has been created successfully")]
			RS_OK =0,
			[Description("Photoscene has been created successfully")]
			RS_OK1 =1,
			[Description("Photoscene computed but some images are missing to the final scene (images corrupted or failed to upload)")]
			RS_OK_MISSING_SHOTS =2,
			[Description("Some of the images appear to be taken from the same physical location. Images in a photo scene cannot be taken from a single position.")]
			RS_OK_PANORAMA =3,
			[Description("Some of the images could not be stitched.")]
			RS_OK_BAD_MATCHING =4,
			[Description("Not used")]
			NOTUSED5 =5,
			[Description("Not used")]
			NOTUSED6 =6,
			[Description("The application was unable to compute the Photoscene")]
			RS_ERROR =7,
			[Description("You must provide at least three images to compute the scene.")]
			RS_ERROR_NOT_ENOUGH_SHOTS =8,
			[Description("Some of the images are corrupt or failed to upload. Check your files and try resubmitting the scene.")]
			RS_ERROR_MISSING_SHOTS =9,
			[Description("No 3D information could be extracted from your images, as they appear to be taken from the same physical location. Images in a photo scene cannot be taken from a single position. Try capturing images of your subject from different positions and resubmit the scene.")]
			RS_ERROR_PANORAMA =10,
			[Description("No 3D information could be extracted from your images, as they do not appear to overlap. Images in a photo scene must overlap. Try capturing overlapping images of your subject and resubmit the scene.")]
			RS_ERROR_BAD_MATCHING =11,
			[Description("There was not enough overlap in your images.")]
			P_ERR_TOO_FEWPOINTS =12,
			[Description("Unable to create Photoscene from your selection region; however, a photo scene was successfully created using the extents of the scene.")]
			KP_ERR_BBOX_EMPTY =13,
			[Description("Not enough of your images could be stitched together.")]
			KP_ERR_TOOFEWCAMERAS =14,
			[Description("No Description")]
			KP_ERR_DOWNSIZENEEDED =15,
		} ;
		public enum MeshQuality {
			[Description ("for draft mesh (default)")]
			DRAFT =7,
			[Description ("for standard mesh")]
			STANDARD =8,
			[Description ("for a high quality mesh")]
			HIGH =9 
		} ;
		public enum FileType {
			Image, Xref, Project, Thumbnail
		}

		#endregion

		#region Member variables
		protected string _clientID ;
		//protected Dictionary<string, string> _tokens =null ;
		private RestClient _restClient =null ;
		private AdskRESTful _adskRESTClient =null ; // Temporary workaround to the DeleteScene issue
		public IRestResponse _lastResponse =null ;

		#endregion

		public AdskReCap (string clientID, Dictionary<string, string> tokens, string apiURL ="http://rc-api-adn.autodesk.com/3.1/API/") {
			_clientID =clientID ;
			// @"oauth_consumer_key" @"oauth_consumer_secret" @"oauth_token" @"oauth_token_secret"
			//_tokens =tokens ;
			_restClient =new RestClient (apiURL) ;
			_restClient.Authenticator =OAuth1Authenticator.ForProtectedResource (
				tokens ["oauth_consumer_key"], tokens ["oauth_consumer_secret"],
				tokens ["oauth_token"], tokens ["oauth_token_secret"]
			) ;

			// Temporary workaround to the DeleteScene issue
			_adskRESTClient =new AdskRESTful (apiURL, null) ;
			_adskRESTClient.addSubscriber (new AdskOauthPlugin (tokens)) ;
		}

		public AdskReCap (string clientID, string consumerKey, string consumerSecret, string oauth, string oauthSecret, string apiURL ="http://rc-api-adn.autodesk.com/3.1/API/") {
			_clientID =clientID ;
			// @"oauth_consumer_key" @"oauth_consumer_secret" @"oauth_token" @"oauth_token_secret"
			//_tokens =new Dictionary<string, string> () {
			//	{ "oauth_consumer_key", consumerKey }, { "oauth_consumer_secret", consumerSecret },
			//	{ "oauth_token", oauth }, { "oauth_token_secret", oauthSecret }
			//}) ;
			_restClient =new RestClient (apiURL) ;
			_restClient.Authenticator =OAuth1Authenticator.ForProtectedResource (
				consumerKey, consumerSecret, oauth, oauthSecret
			) ;

			// Temporary workaround to the DeleteScene issue
			_adskRESTClient =new AdskRESTful (apiURL, null) ;
			_adskRESTClient.addSubscriber (new AdskOauthPlugin (new Dictionary<string, string> () {
				{ "oauth_consumer_key", consumerKey }, { "oauth_consumer_secret", consumerSecret },
				{ "oauth_token", oauth }, { "oauth_token_secret", oauthSecret }
			})) ;
		}

		#region ReCap API interface
		/// <summary>
		/// Return the server date and time
		/// </summary>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/service/get_date_get_1</para>
		/// <para>On Error: </para>
		/// </returns>
		public async Task<bool> ServerTime (bool json =false) {
			var request =new RestRequest ("service/date", Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("service/date response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Return the server API version
		/// </summary>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/</para>
		/// <para>On Error: </para>
		/// </returns>
		public async Task<bool> Version (bool json =false) {
			var request =new RestRequest ("version", Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("version response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Return the ReCap userID
		/// </summary>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/</para>
		/// <para>On Error: </para>
		/// </returns>
		public async Task<bool> User (string email, string o2id, bool json =false) {
			var request =new RestRequest ("user", Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("email", email) ;
			request.AddParameter ("O2ID", o2id) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("user response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Allow the client to set the template he wants to use in emails sent when photoscene is available.
		/// If a photoscene is in success state, the 'DONE' email will be sent, if not the 'ERROR' will be.
		/// </summary>
		/// <param name="emailType">Type of email template. Principal template are for DONE or ERROR projects.</param>
		/// <param name="msg">Content of email template. Do not exceed 1024 chars.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/notification/setTemplateEmail_post_1</para>
		/// <para>On Error: BAD_RESOURCE, DB_ERROR</para>
		/// </returns>
		public async Task<bool> SetNotificationMessage (NotificationType emailType, string msg, bool json =false) {
			var request =new RestRequest ("notification/template", Method.POST) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("emailType", emailType.ToString ()) ;
			request.AddParameter ("emailTxt", msg) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("notification/template response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Create a scene with basic parameters
		/// </summary>
		/// <param name="format">The scene format: 3DP (default), RCP, FBX, IPM, LAS, FYSC, RCS, RCM, OBJ (as a tar.gz package)</param>
		/// <param name="meshQuality">7 for draft mesh (default), 8 for standard and 9 for a high quality mesh.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/create_complete_photoscene_post_0</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID, BAD_RESOURCE, SCENE_NAME_EMPTY, USER_NOT_IDENTIFIED, CANNOT_ALLOCATE_PHOTOSCENE_ID, DB_ERROR</para>
		/// </returns>
		public async Task<bool> CreateSimplePhotoscene (Format format =Format._3DP, MeshQuality meshQuality =MeshQuality.DRAFT, bool json =false) {
			var request =new RestRequest ("photoscene", Method.POST) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("format", format.ToFriendlyString ().ToLower ()) ;
			request.AddParameter ("meshquality", ((int)meshQuality).ToString ()) ;
			request.AddParameter ("scenename", string.Format ("MyPhotoScene{0}", AdskRESTful.timestamp ())) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Create a scene with advanced parameters
		/// </summary>
		/// <param name="format">The scene format: 3DP (default), RCP, FBX, IPM, LAS, FYSC, RCS, RCM, OBJ (as a tar.gz package)</param>
		/// <param name="meshQuality">7 for draft mesh (default), 8 for standard and 9 for a high quality mesh.</param>
		/// <param name="options">.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/create_complete_photoscene_post_0</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID, BAD_RESOURCE, SCENE_NAME_EMPTY, USER_NOT_IDENTIFIED, CANNOT_ALLOCATE_PHOTOSCENE_ID, DB_ERROR</para>
		/// </returns>
		public async Task<bool> CreatePhotoscene (Format format, MeshQuality meshQuality, Dictionary<string, string> options, bool json =false) {
			var request =new RestRequest ("photoscene", Method.POST) ;
			request.AddParameter ("clientID", _clientID);
			request.AddParameter ("format", format.ToFriendlyString ().ToLower ()) ;
			request.AddParameter ("meshquality", ((int)meshQuality).ToString ()) ;
			request.AddParameter ("scenename", string.Format ("MyPhotoScene{0}", AdskRESTful.timestamp ())) ;
			foreach ( KeyValuePair<string, string> entry in options )
				request.AddParameter (entry.Key, entry.Value) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Returns properties for all photoscenes ordered by creationDate desc using filter (attributeName/attributeValue).
		/// </summary>
		/// <param name="attributeName">'Advanced tool':to specify name of field in Database to apply 'attributeValue' to filter.</param>
		/// <param name="attributeValue">'Advanced tool':to specify value of field in Database to filter using 'attributeName' name.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_properties_get_9</para>
		/// <para>On Error: </para>
		/// </returns>
		public async Task<bool> SceneList (string attributeName, string attributeValue, bool json =false) {
			var request =new RestRequest ("photoscene/properties", Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("attributeName", attributeName) ;
			request.AddParameter ("attributeValue", attributeValue) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/properties response: {0}", _lastResponse) ; // Can be very big
			Log ("photoscene/properties response: <returned with a response>", "Response") ;
			return (isOk ()) ;
		}
		
		/// <summary>
		/// All the data associated to the given photoscene(s) and a list of files used for given photoscene(s).
		/// </summary>
		/// <param name="photosceneid">The ID of the photoscene to get properties.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_properties_get_9</para>
		/// <para>On Error: BAD_SCENE_ID</para>
		/// </returns>
		public async Task<bool> SceneProperties (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}/properties", photosceneid), Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/.../properties response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// All the data associated to the given photoscene(s) and a list of files used for given photoscene(s).
		/// </summary>
		/// <param name="photosceneid">The ID of the photoscene to get properties.</param>
		/// <param name="files">The list of image files to upload.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_properties_get_9</para>
		/// <para>On Error: BAD_SCENE_ID, SCENE_ALREADY_PROCESSED, CANT_COPY_FILE, DB_ERROR, BAD_VALUES, PHOTOSCENE_CORRUPTED, BAD_IMAGE_PROTOCOL, DB_BAD_ID</para>
		/// </returns>
		public async Task<bool> UploadFiles(string photosceneid, Dictionary<string, string> files, bool json =false) {
			// ReCap returns the following if no file uploaded (or referenced), setup an error instead
			//<Response>
			//        <Usage>0.81617307662964</Usage>
			//        <Resource>/file</Resource>
			//        <photosceneid>  your scene ID  </photosceneid>
			//        <Files>
			//
			//        </Files>
			//</Response>
			if ( files == null || files.Count == 0 ) {
				_lastResponse =null ;
				return (false) ;
			}
			var request =new RestRequest (string.Format ("file", photosceneid), Method.POST) ;
			request.Timeout =1 * 60 * 60 * 1000 ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter ("type", "image") ;
			int n =0 ;
			foreach ( KeyValuePair<string, string> entry in files ) {
				string key =string.Format ("file[{0}]", n++) ;
				if ( File.Exists (entry.Value) ) {
					request.AddFile (key, entry.Value) ;
				} else if ( entry.Value.Substring (0, 4).ToLower () == "http" || entry.Value.Substring (0, 3).ToLower () == "ftp" ) {
					request.AddParameter (key, entry.Value) ;
				} else {
					byte [] img =Convert.FromBase64String (entry.Value) ;
					request.AddFile (key, img, entry.Key) ;
				}
			}
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("file response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		public RestRequestAsyncHandle UploadFilesAsync (string photosceneid, Dictionary<string, string> files, Action<IRestResponse, RestRequestAsyncHandle> callback) {
			// ReCap returns the following if no file uploaded (or referenced), setup an error instead
			//<Response>
			//        <Usage>0.81617307662964</Usage>
			//        <Resource>/file</Resource>
			//        <photosceneid>  your scene ID  </photosceneid>
			//        <Files>
			//
			//        </Files>
			//</Response>
			if ( files == null || files.Count == 0 ) {
				_lastResponse =null ;
				return (null) ;
			}
			var request =new RestRequest (string.Format ("file", photosceneid), Method.POST) ;
			request.Timeout =1 * 60 * 60 * 1000 ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter ("type", "image") ;
			int n =0 ;
			if ( files != null ) {
				foreach ( KeyValuePair<string, string> entry in files ) {
					string key =string.Format ("file[{0}]", n++) ;
					if ( File.Exists (entry.Value) ) {
						request.AddFile (key, entry.Value) ;
					} else if ( entry.Value.Substring (0, 4).ToLower () == "http" || entry.Value.Substring (0, 3).ToLower () == "ftp" ) {
						request.AddParameter (key, entry.Value) ;
					} else {
						byte [] img =Convert.FromBase64String (entry.Value) ;
						request.AddFile (key, img, entry.Key) ;
					}
				}
			}
			// Inline example
			//var asyncHandle =_restClient.ExecuteAsync (request, response => { if ( response.StatusCode == HttpStatusCode.OK ) {} else {} }) ;
			var asyncHandle =_restClient.ExecuteAsync (request, callback) ;
			System.Diagnostics.Debug.WriteLine ("async file call started") ;
			return (asyncHandle) ;
		}

		/// <summary>
		/// Launch the Photoscene process.
		/// This method has to be called after creating a Photoscene and uploading the images (if needed).
		/// You can monitor the progress of the processing using the GetPhotosceneProgress call.
		/// This method will always force the scene to be processed even if this photoscene has already been processed.
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/process_a_photoscene_post_3</para>
		/// <para>On Error: BAD_SCENE_ID, PERMISSION_DENIED, USER_NOT_IDENTIFIED, NOT_ENOUGH_IMAGES, CANT_COPY_FILE, CANT_READ_PHOTOSCENE_FILE, NO_RIGHTS, CANNOT_SEND_MESSAGE</para>
		/// </returns>
		public async Task<bool> ProcessScene (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}", photosceneid), Method.POST) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter ("forceReprocess", "1") ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("(post) photoscene/... response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Returns the current progress percentage of a photoscene.
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_progress_get_6</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID, PERMISSION_DENIED</para>
		/// </returns>
		public async Task<bool> SceneProgress (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}/progress", photosceneid), Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/.../progress response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Return time in second to calculate the given photoscene.
		/// If the scene has not yet been computed, 0 is returned. the time in second to process the photoscene.
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_processingtime_get_7</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID</para>
		/// </returns>
		public async Task<bool> ProcessingTime (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}/processingtime", photosceneid), Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/.../processingtime response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Return the size on disc for all documents used to or created by given photoscene.
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_processingtime_get_7</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID</para>
		/// </returns>
		public async Task<bool> FileSize (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}/filesize", photosceneid), Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/.../filesize response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Return the given file
		/// </summary>
		/// <param name="fileid">The ID of the file to get</param>
		/// <param name="fileType">If provided only this specific type is search for. If not provided the API will
		/// try to do a best guess on the file type. Can be 'image' or 'xref'.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns></returns>
		public async Task<bool> GetFile (string fileid, FileType fileType =FileType.Image, bool json =false) {
			var request = new RestRequest (string.Format ("file/{0}/get", fileid), Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("type", fileType.ToString ().ToLower ()) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("file/.../get response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Get the given Photoscene as a link.
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="format">The scene format: 3DP (default), RCP, FBX, IPM, LAS, FYSC, RCS, RCM, OBJ (as a tar.gz package)</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/get_photoscene_get_4</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID, NO_RIGTHS, NOT_READY</para>
		/// </returns>
		public async Task<bool> GetPointCloudArchive (string photosceneid, Format format, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}", photosceneid), Method.GET) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter ("format", format.ToFriendlyString ().ToLower ()) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/... response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Set the Photoscene status to CANCEL for no further processing
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/cancel_a_photoscene_post_10</para>
		/// <para>On Error: BAD_SCENE_ID, PERMISSION_DENIED</para>
		/// </returns>
		public async Task<bool> Cancel (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}/cancel", photosceneid), Method.POST) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/.../cancel response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Set the Photoscene status to CANCEL for no further processing
		/// </summary>
		/// <param name="photosceneid">The photoscene ID to identify photoscene.</param>
		/// <param name="clientError">The error to set to the photoscene.</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/cancel_a_photoscene_post_10</para>
		/// <para>On Error: BAD_SCENE_ID, EMPTY_RESOURCE_ID, DB_ERROR</para>
		/// </returns>
		public async Task<bool> SetError (string photosceneid, string clientError, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}/cancel", photosceneid), Method.POST) ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter ("photosceneid", photosceneid) ;
			request.AddParameter ("clientError", clientError) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			Log ("photoscene/.../error response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Delete the given photoscene and all the associated assets (images, output files, ...)
		/// </summary>
		/// <param name="photosceneid">The ID of the photoscene to get properties</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/delete_photoscene_delete_5</para>
		/// <para>On Error: EMPTY_RESOURCE_ID, BAD_SCENE_ID</para>
		/// </returns>
		public async Task<bool> DeleteScene (string photosceneid, bool json =false) {
			var request =new RestRequest (string.Format ("photoscene/{0}", photosceneid), Method.DELETE) ;
			request.AlwaysMultipartFormData =true ;
			request.AddParameter ("clientID", _clientID) ;
			request.AddParameter (json ? "json" : "xml", 1) ;
			Log (String.Format ("{0} {1} request sent", request.Method, request.Resource)) ;
			_lastResponse =await _restClient.ExecuteTaskAsync (request) ;
			//var method =Enum.GetName (typeof (Method), Method.DELETE) ;
			//_lastResponse =_restClient.ExecuteAsPost (request, method) ; // sign as POST :(
			Log ("(delete) photoscene/... response: {0}", _lastResponse) ;
			return (isOk ()) ;
		}

		/// <summary>
		/// Delete the given photoscene and all the associated assets (images, output files, ...)
		/// </summary>
		/// <param name="photosceneid">The ID of the photoscene to get properties</param>
		/// <param name="json">true to receive the response in JSON format. Otherwise default is XML.</param>
		/// <returns><para>http://rc-api-adn.autodesk.com/3.1/api-docs/#!/photoscene/delete_photoscene_delete_5</para>
		/// <para>On Error: EMPTY_RESOURCE_ID, BAD_SCENE_ID</para>
		/// </returns>
		public bool DeleteSceneTempFix (string photosceneid) {
			_adskRESTClient.AlwaysMultipart =true ;
			_adskRESTClient.AlwaysSignParameters =true ;
			_adskRESTClient.clearAllParameters () ;
			_adskRESTClient.addParameters (new Dictionary<string, string> () {
				{ "clientID", _clientID }
			}) ;
			HttpWebRequest req =_adskRESTClient.delete (string.Format ("photoscene/{0}", photosceneid), null) ;
			AdskRESTfulResponse response =_adskRESTClient.send (req) ;
			using ( StreamReader reader =new StreamReader (response.urlResponse.GetResponseStream ()) ) {
				// Get the response stream and write to console
				string text =reader.ReadToEnd () ;
				System.Diagnostics.Debug.WriteLine ("Successful Response: \r\n" + text) ;
				_lastResponse.StatusCode =response.urlResponse.StatusCode ;
				_lastResponse.Content =text ;
				_lastResponse.ContentLength =text.Length ;
			}
			_adskRESTClient.AlwaysMultipart =false ;
			_adskRESTClient.AlwaysSignParameters =false ;
			return (isOk ()) ;
		}

		#endregion

		#region Utilities
		public string ErrorMessage () {
			if ( _lastResponse == null )
				return ("") ;
			string errmsg ="" ;
			if ( _lastResponse.ErrorMessage != null && _lastResponse.ErrorMessage != "" ) {
				errmsg =_lastResponse.ErrorMessage ;
			} else {
				XmlDocument xmlDoc =xml () ;
				if ( xmlDoc != null ) {
					XmlNode errorCode =xmlDoc.SelectSingleNode ("/Response/Error/code") ;
					XmlNode results =xmlDoc.SelectSingleNode ("/Response/Error/msg") ;
					errmsg =string.Format ("{0} (# {1})", results.InnerText, errorCode.InnerText) ;
				} else {
					errmsg ="Not an XML response." ;
				}
			}
			return (errmsg) ;
		}

		public bool isXml () {
			if ( _lastResponse == null || _lastResponse.StatusCode != HttpStatusCode.OK )
				return (false) ;
			string st =this.ToString () ;
			return (st.IndexOf ("<?xml") != -1) ;
		}

		public XmlDocument xml () {
			if ( _lastResponse == null || _lastResponse.ErrorMessage != null )
				return (null) ;
			try {
				XmlDocument theDocument =new XmlDocument () ;
				theDocument.LoadXml (ToString ()) ;
				return (theDocument) ;
			} catch /*( Exception ex )*/ {
				Log ("Not a valid XML response.", "Error") ;
				return (null) ;
			}
		}

		public XDocument xmlLinq () {
			if ( _lastResponse == null || _lastResponse.ErrorMessage != null )
				return (null) ;
			try {
				XDocument theDocument =XDocument.Parse (ToString ()) ;
				return (theDocument) ;
			} catch /*( Exception ex )*/ {
				Log ("Not a valid XML response.", "Error") ;
				return (null) ;
			}
		}

		public JObject json () {
			if ( _lastResponse == null || _lastResponse.ErrorMessage != null )
				return (null) ;
			try {
				JObject theDocument =JObject.Parse (ToString ()) ;
				return (theDocument) ;
			} catch /*( Exception ex )*/ {
				Log ("Not a valid JSON response.", "Error") ;
				return (null) ;
			}
		}

		public AdskReCapResponse response () {
			if ( _lastResponse == null || _lastResponse.StatusCode != HttpStatusCode.OK )
				return (null) ;
			if ( isXml () )
				return (new AdskReCapResponse (xmlLinq ())) ;
			else
				return (new AdskReCapResponse (json ())) ;
		}

		public bool isOk () {
			if ( _lastResponse == null || _lastResponse.StatusCode != HttpStatusCode.OK )
				return (false) ;
			string st =this.ToString () ;
			return (st.IndexOf ("<error>") == -1 && st.IndexOf ("<Error>") == -1) ;
		}

		override public string ToString () {
			if ( _lastResponse == null )
				return ("<error>") ;
			return (_lastResponse.Content) ;
		}

		private static void Log (string format, IRestResponse message) {
			Log (string.Format (format, message.Content), "Response") ;
		}

		private static void Log (string msg) {
			Log (msg, "Request") ;
		}

		private static void Log (string msg, string category ="Info") {
			Trace.WriteLine ("ReCap: " + msg, category) ;
		}

		#endregion

	}

}
