# Quest App Launcher

An app launcher for Quest implemented in Unity.

## Overriding app icons and names
By default, the launcher uses the default app name and icon (from the apk). Sometimes, however, it is desirable to override these - for example, the icon may be the default Oculus icon instead of the actual game icon; or the app name may these do not map to the actual app name and icon in the Oculus Store.

### Override app names
Create a file called **appnames.txt**. Add a line per app with comma-separated package-id and desired name. Example:  
appnames.txt:  
com.mycompany.myapp,My Application  
com.othercompany.otherapp,Other application  

Copy this file (appnames.txt) to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files

### Override app icons
Create a jpg file per app with the package-id as the filename. Example:  
com.mycompany.myapp.jpg  
com.thirdcompany.yetanotherapp.jpg  

Copy these files to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files

## Configuration
The app can be customized by creating a **config.json** file and copying it to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files.  

The following options are supported:  
### Categories (tabs)
By default, the launcher will display 3 tabs - Quest, Go/GrearVr and 2D. This can be overridden by specifying the field "categoryType" with one of the following values:

"none": No categories - all apps are listed in a single pane
"auto": Apps are automatically categorized into 3 tabs - Quest, Go/GearVr, 2D
"custom": Apps are categorized according to appnames.txt file - see below

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

### Only showing apps specified in appnames.txt
By default, the launcher will show all installed apps. Use this "showOnlyCustom" option to exclude any apps that are not specified appnames.txt. This is useful for organizing the launcher with a highly curated list of apps.

### Example config.json
Here's an example config.json:

```
{
    "gridSize": {
        "rows": 3,
        "cols": 4
    },
    "show2D": true,
    "showOnlyCustom": false,
    "categoryType": "custom"
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
