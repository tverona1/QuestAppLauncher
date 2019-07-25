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
using UnityEngine.SceneManagement;

namespace QuestAppLauncher
{
    /// <summary>
    /// </summary>
    public class GridPopulation : MonoBehaviour
    {
        // File name of app name overrides
        const string AppNameOverrideFile = "appnames.txt";

        // File name of excluded package names
        const string ExcludedPackagesFile = "excludedpackages.txt";

        // Extension search for icon overrides
        const string IconOverrideExtSearch = "*.jpg";

        // Grid container game object
        public GameObject gridContainer;

        // Scroll view game object
        public GameObject scrollView;

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

            // Process configuration
            StartCoroutine(ProcessConfig());

            // Populate the grid
            StartCoroutine(Populate());
        }

        void Update()
        {
        }
        #endregion

        #region Private Functions
        
        /// <summary>
        /// Static method for launching an Android app
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
        /// Static method to add a package name to the excludedFile
        /// </summary>
        /// <param name="packageName"></param>
        static public void AddAppToExcludedFile(string packageName)
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);

            using (StreamWriter writer = File.AppendText(excludedPackageNamesFilePath))
            {
                writer.WriteLine(packageName);
                Debug.Log($"Added package {packageName} to the excluded file {excludedPackageNamesFilePath}");
            }
        }

        /// <summary>
        /// Static method to delete the excludedFile
        /// </summary>
        /// <param name="packageName"></param>
        static public void DeleteExcludedApksFile()
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);

            if (File.Exists(excludedPackageNamesFilePath))
                File.Delete(excludedPackageNamesFilePath);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Loads & processes config
        /// </summary>
        /// <returns></returns>
        IEnumerator ProcessConfig()
        {
            // Load configuration
            Config config = new Config();
            ConfigPersistence.LoadConfig(config);
            SetGridSize(config.gridSize.rows, config.gridSize.cols);

            yield return null;
        }

        public void SetGridSize(int rows, int cols)
        {
            // Make sure grid size have sane value
            cols = Math.Min(cols, 10);
            cols = Math.Max(cols, 1);
            rows = Math.Min(rows, 10);
            rows = Math.Max(rows, 1);

            // Get cell size, spacing & padding from the grid layout
            var gridLayoutGroup = this.gridContent.GetComponent<GridLayoutGroup>();
            var cellHeight = gridLayoutGroup.cellSize.y;
            var cellWidth = gridLayoutGroup.cellSize.x;
            var paddingX = gridLayoutGroup.padding.horizontal;
            var paddingY = gridLayoutGroup.padding.vertical;
            var spaceX = gridLayoutGroup.spacing.x;
            var spaceY = gridLayoutGroup.spacing.y;

            // Width = horizontal padding + # cols * cell width + (# cols - 1) * horizontal spacing
            var width = paddingX + cols * cellWidth + (cols - 1) * spaceX;

            // Height = vertical padding + # rows * cell height + (# rows - 1) * veritcal spacing + a bit more to show there are more elements
            var height = paddingY + rows * cellHeight + (rows - 1) * spaceY + 120;

            Debug.Log(string.Format("Setting grid size to {0} x {1} cells", cols, rows));
            Debug.Log(string.Format("Grid size calculated width x height: {0} x {1}", width, height));

            // Adjust grid container rect transform
            var gridTransform = this.gridContainer.GetComponent<RectTransform>();
            gridTransform.sizeDelta = new Vector2(width, height);

            // Adjust grid container Y position to maintain constant height.
            // TODO: Figure out a way to adjust UI to avoid this calculation in code
            var gridPosition = new Vector3(gridTransform.anchoredPosition3D.x,
                (float)((gridTransform.rect.height - 2000) / 2.0),
                gridTransform.anchoredPosition3D.z);
            gridTransform.anchoredPosition3D = gridPosition;

            // Adjust scroll view rect transform
            var scrollViewRectTransform = this.scrollView.GetComponent<RectTransform>();
            scrollViewRectTransform.sizeDelta = new Vector2(width, height);
            
            // Adjust scroll view box collider
            var scrollViewBoxCollider = this.scrollView.GetComponent<BoxCollider>();
            scrollViewBoxCollider.size = new Vector3(width, height, 0);
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
                var excludedPackageNames = new HashSet<string>();

                // Get # of installed apps
                int numApps = currentActivity.Call<int>("getSize");
                Debug.Log("# installed apps: " + numApps);

                // Add current package name to excludedPackageNames to filter it out
                excludedPackageNames.Add(currentActivity.Call<string>("getPackageName"));

                // This is a file containing packageNames that will be excluded
                var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);
                if (File.Exists(excludedPackageNamesFilePath))
                {
                    Debug.Log("Found file: " + excludedPackageNamesFilePath);
                    string[] excludedPackages = File.ReadAllLines(excludedPackageNamesFilePath);
                    foreach (string excludedPackage in excludedPackages)
                    {
                        if (!string.IsNullOrEmpty(excludedPackage))
                        {
                            excludedPackageNames.Add(excludedPackage);
                        }
                    }
                }

                // Get installed package and app names
                for (int i = 0; i < numApps; i++)
                {
                    var packageName = currentActivity.Call<string>("getPackageName", i);
                    var appName = currentActivity.Call<string>("getAppName", i);

                    if (excludedPackageNames.Contains(packageName))
                    {
                        Debug.Log("Skipping [" + i + "] package: " + packageName + ", name: " + appName);
                        // Skip exluded package
                        continue;
                    }

                    packageNameToAppName.Add(packageName, (i, appName));
                    Debug.Log("[" + i + "] package: " + packageName + ", name: " + appName);
                    yield return null;
                }

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

                        yield return null;
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
