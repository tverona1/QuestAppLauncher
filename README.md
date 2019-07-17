# Quest App Launcher

An app launcher for Quest implemented in Unity.

##Overriding app icons and names
For each installed app, we extract the default app name (PackageManager.getApplicationLabel()) and default app icon (PackageManager.getApplicationIcon()). Sometimes, however, these do not map to the actual app name and icon in the Oculus Store. In order to override this, do the following:

1) Override app names: Create a file called appnames.txt. Add a line per app with comma-separated package-id and desired name. Example:

appnames.txt
------------
com.mycompany.myapp,My Application
com.othercompany.otherapp,Other application

2) Override app icons: Create a jpg file per app with the package-id as the filename. Example:
com.mycompany.myapp.jpg
com.thirdcompany.yetanotherapp.jpg

3) Copy the above contents (appnames.txt + jpg files) to the following location: Android/data/aaa.QuestAppLauncher.App/files

##Source structure:
- Assets/Scenes/QuestAppLauncher.unity: The main scene
- Assets/Plugins/Android: Android-specific implementation to retrieve installed apps etc, written in Java
- Assets/Scripts: Main set of C# scripts for grid population, scroll handling etc.
