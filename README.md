# Quest App Launcher

An app launcher for Quest implemented in Unity.

## Overriding app icons and names
For each installed app, we extract the default app name (PackageManager.getApplicationLabel()) and default app icon (PackageManager.getApplicationIcon()). Sometimes, however, these do not map to the actual app name and icon in the Oculus Store. In order to override this, do the following:

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

## Source structure:
- Assets/Scenes/QuestAppLauncher.unity: The main scene
- Assets/Plugins/Android: Android-specific implementation to retrieve installed apps etc, written in Java
- Assets/Scripts: Main set of C# scripts for grid population, scroll handling etc.
