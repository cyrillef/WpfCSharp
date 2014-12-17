The WPF C# sample
=======================

<b>Note:</b> For using those samples you need a valid oAuth credential and a ReCap client ID.  Visit this [page](http://developer-recap-autodesk.github.io/) for instructions to get on-board.


Dependencies
--------------------
This sample is dependent of five 3rd party assemblies which would update/install automatically via NuGet:

1. The RestSharp assembly

     RestSharp - Simple REST and HTTP Client for .NET
	 you need at least version 104.3.3. You can get this component source code [here](http://restsharp.org/)

2. The ObjLoader from Chris Janson

     you need this assembly for the obj file format preview. You can get this component source code [here](https://github.com/ChrisJansson/ObjLoader)

3. The Xceed WPF Toolkit Community Edition

     this assembly is only used if you want to display the ReCap properties in a Property window. Properties are anyway dumped into a text window. 
	 You can get the binaries and documentation from [https://wpftoolkit.codeplex.com/](https://wpftoolkit.codeplex.com/)

4. The Autodesk Dark color theme assembly

     you can either remove or use that assembly as you wish since it only change the WPF window colors.
	 You can get the source code [here](https://github.com/ADN-DevTech/Maya-Net-Wpf-DarkScheme)

5. The Json.NET library

	 You can get this library from http://james.newtonking.com/json and/or use NuGet to install it for the project. See instructions here: http://www.nuget.org/packages/newtonsoft.json/

	 
Building the sample
---------------------------

The sample was created using Visual Studio 2012 Service Pack 1, but should build/work fine with Visual Studio 2010 and 2013.

You first need to modify (or create) the UserSettings.cs file and put your oAuth /ReCap credentials in it.
	 
Use of the sample
-------------------------

* when you launch the sample, the application will try to connect to the ReCap server and verifies that you are properly authorized on the Autodesk oAuth server. 
If you are, it will refresh your access token immediately. If not, it will ask you to get authorized. Once you are authorized, close the oAuth dialog to continue.

* Shots Panel - you can Drag'nDrop images into the 'Photos' view and select the photos/shots you want. 

   * You can then create a new PhotoScene or add them to an existing PhotoScene. To create a new PhotoScene, right-click and select 'New PhotoScene from'
   the context menu. To add photos to an existing PhotoScene, switch to the 'ReCap Project' tab, select a project and choose 'Upload Photo(s)' from the context menu.
   * 'Remove' and 'Remove All' menus are to remove Photos from the list. They do not delete photos from PhotoScenes and/or your hard-drive.
   * The 'Presets' buttons are for helping the developers to debug - please ignore for now.
   
* ReCap Project Panel - the application will list all your projects from the ReCap server when you first open that tab.

   * Properties - You can get Project' properties from the server  by using the context menu and choosing 'Properties'. That will open a new dialog after the server returned the data.
   You can also call this method as many time you want as properties can be queried anytime.
   * Process Scene -  This command will ask the ReCap server to process the PhotoScene. You need a minimum of 3 photos to launch the processing, and this command is configured
   to for re-processing in case the scene was already processed.
   * Download result - If a PhotoScene was successfully processed, you can get the resulting ZIP file containing the mesh, texture, and material. The sample is configured to use OBJ file format.
   Other  file format will be supported in future whenever possible.
   * Preview - This command load the PhotoScene mesh into the 'Polygon 3D View' panel.
   * Delete - This command will ask the ReCap server to delete the PhotoScene and all its resources from the Autodesk server.
   
* Polygon 3D View - simple mesh preview with basic orbit functionality


--------

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.


## Written by

Cyrille Fauvel (Autodesk Developer Network)  
http://www.autodesk.com/adn  
http://around-the-corner.typepad.com/  
