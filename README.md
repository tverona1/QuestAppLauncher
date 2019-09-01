# Quest App Launcher

An app launcher for Quest implemented in Unity.

## Features
* Supports launching both 3D and 2D apps
* Auto-categorizes apps as Quest, Go / GearVR or 2D
* Support for custom categorization
* Support for custom app icons and names (by default, uses app name and icon from the app's apk)
* Support for resizable grid
* Support for downloading app icons and names, including auto-updating
* Support for sorting alphabetically or by most recently used

## Getting Started

### Downloading app icons and names / enabling auto-updating
By default, the launcher uses the built-in app name and icon (from the installed apps). Oculus Store apps, however, sometimes do not contain correct names or icons. To address this, download these assets by going to Settings and choosing "Update Now". To enable auto-updates, enable "Auto-Update". This will automatically download new app names and icons as they become available.

Note: Default repository for app icons and names is [https://github.com/tverona1/QuestAppLauncher_assets]. This can be configured in config.json (see below).

### Settings
There are various options available in Settings. These are:
* Automatic Tabs: By default, the launcher will display automatic tabs (Quest, Go/GrearVr and 2D). This setting allows you to specify the position of these tabs (left, top, right or off).
* Custom Tabs: Custom tabs allow for artibtary categorization (like genre etc). See below for how to configure these. This setting, like the above, allows you to specify the position of these tabs (left, top, right or off).
* Sort By: Whether to sort alphabetically or most recent.
* Show 2D: Whether to show 2D apps.
* Grid Size: How many columns & rows to display.
* Auto Update & Update Now: Whether to auto-update app icons & names and whether to update these now -- see above.
* Reset Hidden Apps: This setting un-hides any hidden apps. (To hide an app, highlight the app on the grid and press either the A or Y button on your controller.)

## Advanced Features
The below sections described advanced / custom features.

### Manually overriding app icons and names
It is also possible to manually configure app icons and names rather than automatically downloading them. This section describes how.

#### Override app names
Create a file called **appnames.json**. Add an entry with package-id as key and the desired name. Example:  
```
"com.company.myapp":{"name": "My Application"}
"com.othercompany.myapp":{"name": "Other Application"}
```

Copy this file (appnames.json) to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files

Note: Multiple appnames*.json files are supported. If multiple files are present, they are applied in sorted order of the filenames. This allows for a single "master" appnames.json file and then additional files to override it (like appnames_custom.json).

Note: Text format is also supported. See below.

#### Override app icons
Create an iconpack.zip file that contains a jpg file per app with the package-id as the filename. Example:  
com.mycompany.myapp.jpg  
com.thirdcompany.yetanotherapp.jpg  

Copy the iconpack.zip to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files

Note: Multiple iconpack*.zip files are supported. If multiple files are present, they are applied in sorted order of the filenames. This allows for a single "master" iconpack.zip file and then additional files to override it (like iconpack_custom.zip).

Note: Individual jpg files are also supported at the /Android/data/aaa.QuestAppLauncher.app/files path. These override any corresponding jpgs in iconpack*.zip files.

### Configuration
The launcher can be customized by creating a **config.json** file and copying it to the following location on your Quest: Android/data/aaa.QuestAppLauncher.App/files.  

The following options are supported:  
#### Categories (tabs)
By default, the launcher will display automatic tabs (Quest, Go/GrearVr and 2D) and any custom tabs specified in appnames.txt file. These can be overridden with two fields - "autoCategory" and "customCategory". Both fields support the following values:

"off": No not display the categories  
"top": Display categories on top  
"left": Display categories on left-side  
"right": Display categories on right-side  

#### Setting Grid Size
The default grid size is 3x3 cells. The grid size can be customized by specifying grid rows and columns as in the following example:

```
{
	"gridSize": {
		"rows": 2,
		"cols": 4
	}
}
```

#### Sort By
Whether to sort apps alphabetically or by most recently used. Key is "sortMode" and valid values are "az" (default) or "mostRecent".

#### Showing / Hiding 2D apps
To show or hide 2D apps, set the "show2D" field to true (default) or false.

#### Example config.json
Here's an example config.json:

```
{
    "gridSize": {
        "rows": 3,
        "cols": 4
    },
    "sortMode": "mostRecent",
    "show2D": true,
    "autoCategory": "top",
    "customCategory": "right"
    "autoUpdate": true,
    "downloadRepos": [
      {
        "repoUri": "tverona1/QuestAppLauncher_Assets/releases/latest",
        "type": "github"
      }
    ]
}
```

### Appnames.json syntax
This section describes the syntax for appnames.json. The syntax for these entries are as follows:

"com.mycompany.myapp":{"name": "My Application"}

#### Custom Categories (Tabs)
The launcher supports custom categories (tabs). This can be specified in the **appnames.json** file by adding up to two custom categories per entry. The syntax is:  
"com.mycompany.myapp":{"name": "My Application", "category": "Action", "category2" : "Puzzle"}

The above example will categorize the two entries below in Action and Puzzle tabs:

### Appnames.txt syntax
Alternatively, txt format is also supported for appnames instead of json. The syntax is:  
packageName,appName[,category1[, category2]]

Comments can be added to appnames.txt by prepending "#".

## Source structure:
- Assets/Scenes/QuestAppLauncher.unity: The main scene
- Assets/Plugins/Android: Android-specific implementation to retrieve installed apps etc, written in Java
- Assets/Scripts: Main set of C# scripts for grid population, scroll handling etc.

## Credits
A huge thank you to fecheva [https://github.com/fecheva] for creating / maintaining the app names & icon packs!

Also thank you to noxx for creating the app's icon!
