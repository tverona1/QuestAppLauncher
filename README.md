# Quest App Launcher

An app launcher for Quest implemented in Unity.

## Features
* Supports launching both 3D and 2D apps
* Auto-categorizes apps as Quest, Go / GearVR or 2D
* Support for custom categorization
* Support for custom app icons and names (by default, uses app name and icon from the app's apk)
* Support for resizable grid

## Overriding app icons and names
By default, the launcher uses the default app name and icon (from the apk). Sometimes, however, it is desirable to override these - for example, the icon may be the default Oculus icon instead of the actual game icon; or the app name may these do not map to the actual app name and icon in the Oculus Store.

### Override app names
Create a file called **appnames.txt**. Add a line per app with comma-separated package-id and desired name. Example:  
appnames.txt:  
com.mycompany.myapp,My Application  
com.othercompany.otherapp,Other application  

Copy this file (appnames.txt) to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files

Note: Multiple appnames*.txt files are supported. If multiple files are present, they are applied in sorted order of the filenames. This allows for a single "master" appnames.txt file and then additional files to override it (like appnames_custom.txt).

### Override app icons
Create an iconpack.zip file that contains a jpg file per app with the package-id as the filename. Example:  
com.mycompany.myapp.jpg  
com.thirdcompany.yetanotherapp.jpg  

Copy the iconpack.zip to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files

Note: Multiple iconpack*.zip files are supported. If multiple files are present, they are applied in sorted order of the filenames. This allows for a single "master" iconpack.zip file and then additional files to override it (like iconpack_custom.zip).

Note: Individual jpg files are also supported at the /Android/data/aaa.QuestAppLauncher.app/files path. These override any corresponding jpgs in iconpack*.zip files.

## Configuration
The launcher can be customized by creating a **config.json** file and copying it to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files.  

The following options are supported:  
### Categories (tabs)
By default, the launcher will display automatic tabs (Quest, Go/GrearVr and 2D) and any custom tabs specified in appnames.txt file. These can be overridden with two fields - "autoCategory" and "customCategory". Both fields support the following values:

"off": No not display the categories  
"top": Display categories on top  
"left": Display categories on left-side  
"right": Display categories on right-side  

### Setting Grid Size
The default grid size is 3x3 cells. The grid size can be customized by specifying grid rows and columns as in the following example:

```
{
	"gridSize": {
		"rows": 2,
		"cols": 4
	}
}
```

### Showing / Hiding 2D apps
To show or hide 2D apps, set the "show2D" field to true or false

### Example config.json
Here's an example config.json:

```
{
    "gridSize": {
        "rows": 3,
        "cols": 4
    },
    "show2D": true,
    "autoCategory": "top",
    "customCategory": "right"
}
```

## Appnames.txt syntax
This section describes the syntax for appnames.txt entries

### Custom Categories (Tabs)
As described above, the launcher supports custom categories (tabs). This can be specified in the **appnames.txt** file by adding up to two custom categories per entry. The syntax is:  
packageName,appName[,category1[, category2]]

For example, this will categorize the two entries below in Action and Puzzle tabs:

com.mycompany.myapp,My Application,Action  
com.othercompany.otherapp,Other application,Puzzle  

### Comments
Comments can be added to appnames.txt by prepending "#".

## Source structure:
- Assets/Scenes/QuestAppLauncher.unity: The main scene
- Assets/Plugins/Android: Android-specific implementation to retrieve installed apps etc, written in Java
- Assets/Scripts: Main set of C# scripts for grid population, scroll handling etc.
