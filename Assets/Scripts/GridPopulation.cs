using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;
using Oculus.Platform;
using TMPro;

namespace QuestAppLauncher
{
    /// <summary>
    /// </summary>
    public class GridPopulation : MonoBehaviour
    {
        // File name of app name overrides
        const string AppNameOverrideFile = "appnames.txt";

        // Extension search for icon overrides
        const string IconOverrideExtSearch = "*.jpg";

        // Grid content game object
        public GameObject gridContent;

        // App info GameObject (a cell in the grid content)
        public GameObject prefab;

        #region MonoBehaviour handler

        void Start()
        {
            // Set high texture resolution scale to minimize aliasing
            XRSettings.eyeTextureResolutionScale = 2.0f;

            // Initialize the core platform
            Core.AsyncInitialize();

            // Populate the grid
            StartCoroutine(Populate());
        }

        void Update()
        {
        }
        #endregion

        #region Private Functions
        
        /// <summary>
        /// Static method for lauching an Android app
        /// </summary>
        /// <param name="packageId"></param>
        static public void LaunchApp(string packageId)
        {
            using (AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager"))
            {
                AndroidJavaObject launchIntent = null;

                try
                {
                    launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", packageId);
                    ca.Call("startActivity", launchIntent);

                    // Quest doesn't like multiple VR apps running simultaneously. Kill ourselves.
                    UnityEngine.Application.Quit();
                }
                catch (System.Exception e)
                {
                    Debug.Log(string.Format("Failed to launch app {0}: {1}", packageId, e.Message));
                }
                finally
                {
                    if (null != launchIntent)
                    {
                        launchIntent.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Populate the grid from installed apps
        /// </summary>
        /// <returns></returns>
        IEnumerator Populate()
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            Debug.Log("Persistent data path: " + persistentDataPath);

            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // Dictionary to hold package name -> app index, app name
                var packageNameToAppName = new Dictionary<string, (int Index, string AppName)>();

                // Get # of installed apps
                int numApps = currentActivity.Call<int>("getSize");
                Debug.Log("# installed apps: " + numApps);

                // Get current package name (to filter this out))
                var currentPackageName = currentActivity.Call<string>("getPackageName");

                // Get installed package and app names
                for (int i = 0; i < numApps; i++)
                {
                    var packageName = currentActivity.Call<string>("getPackageName", i);
                    var appName = currentActivity.Call<string>("getAppName", i);

                    if (packageName.Equals(currentPackageName))
                    {
                        // Skip current package
                        continue;
                    }

                    packageNameToAppName.Add(packageName, (i, appName));
                    Debug.Log("[" + i + "] package: " + packageName + ", name: " + appName);
                }

                yield return null;

                // Override app names, if any
                // This is just a file with comma-separated packageName,appName pairs
                var appNameOverrideFilePath = Path.Combine(persistentDataPath, AppNameOverrideFile);
                if (File.Exists(appNameOverrideFilePath))
                {
                    Debug.Log("Found file: " + appNameOverrideFilePath);
                    string[] overriddenNames = File.ReadAllLines(appNameOverrideFilePath);
                    foreach (string overriddenName in overriddenNames)
                    {
                        var entry = overriddenName.Split(',');
                        if (2 == entry.Length && packageNameToAppName.ContainsKey(entry[0]))
                        {
                            packageNameToAppName[entry[0]] = (packageNameToAppName[entry[0]].Index, entry[1]);
                        }
                    }
                }
                else
                {
                    Debug.Log("Did not find: " + appNameOverrideFilePath);
                }

                yield return null;

                // Load list of app icon overrides
                // This is a list of jpg images stored as packageName.jpg.
                var iconOverridePath = persistentDataPath;
                var iconOverrides = new Dictionary<string, string>();
                if (Directory.Exists(iconOverridePath))
                {
                    foreach (var iconFileName in Directory.GetFiles(iconOverridePath, IconOverrideExtSearch))
                    {
                        var entry = Path.GetFileNameWithoutExtension(iconFileName);
                        if (packageNameToAppName.ContainsKey(entry))
                        {
                            Debug.Log("Found file: " + iconFileName);
                            iconOverrides[entry] = Path.Combine(iconOverridePath, iconFileName);
                        }
                    }
                }

                yield return null;

                // Populate grid with app information (name & icon)
                // Sort by app name
                foreach (var app in packageNameToAppName.OrderBy(key => key.Value.AppName))
                {
                    // Create new instances of our app info prefab
                    var newObj = (GameObject)Instantiate(prefab, gridContent.transform);

                    // Set app entry info
                    var appEntry = newObj.GetComponent("AppEntry") as AppEntry;
                    appEntry.packageId = app.Key;
                    appEntry.appName = app.Value.AppName;

                    // Get app icon
                    byte[] bytesIcon = null;
                    bool useApkIcon = true;
                    if (iconOverrides.ContainsKey(app.Key))
                    {
                        // Use overridden icon
                        try
                        {
                            bytesIcon = File.ReadAllBytes(iconOverrides[app.Key]);
                            useApkIcon = false;
                        }
                        catch (Exception e)
                        {

                            // Fall back to using the apk icon
                            Debug.Log(string.Format("Error reading app icon from file [{0}]: {1}", iconOverrides[app.Key], e.Message));
                        }
                    }

                    if (useApkIcon)
                    {
                        // Use built-in icon from the apk
                        bytesIcon = (byte[])(Array)currentActivity.Call<sbyte[]>("getIcon", app.Value.Index);
                    }

                    // Set the icon image
                    var image = newObj.transform.Find("AppIcon").GetComponentInChildren<Image>();
                    var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    texture.filterMode = FilterMode.Trilinear;
                    texture.anisoLevel = 16;
                    //texture.Resize(144, 100, TextureFormat.RGB24, false);
                    texture.LoadImage(bytesIcon);
                    var rect = new Rect(0, 0, texture.width, texture.height);
                    image.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                    // Set app name in text
                    var text = newObj.transform.Find("AppName").GetComponentInChildren<TextMeshProUGUI>();
                    text.text = app.Value.AppName;

                    yield return null;
                }
            }
        }
        #endregion
    }
}
