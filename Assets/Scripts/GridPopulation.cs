using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
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

        // File name of excluded package names
        const string ExcludedPackagesFile = "excludedpackages.txt";

        // Extension search for icon overrides
        const string IconOverrideExtSearch = "*.jpg";

        // Grid container game object
        public GameObject panelContainer;

        // Scroll container game object
        public GameObject scrollContainer;

        // Tab container
        public GameObject tabContainer;

        // Tracking space
        public GameObject trackingSpace;

        // App info prefab (a cell in the grid content)
        public GameObject prefabCell;

        // Scroll view prefab
        public GameObject prefabScrollView;

        // Tab prefab
        public GameObject prefabTab;

        // Reference to executing populate routine
        private Coroutine populateCoroutine;

        // Built-in tab names
        private const string Tab_None = "None";
        private const string Tab_Quest = "Quest";
        private const string Tab_Go = "Go/Gear";
        private const string Tab_2D = "2D";

        #region MonoBehaviour handler

        void Start()
        {
            // Set high texture resolution scale to minimize aliasing
            XRSettings.eyeTextureResolutionScale = 2.0f;

            // Initialize the core platform
            Core.AsyncInitialize();

            // Populate the grid
            StartPopulate();
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

        public void SetGridSize(GameObject gridContent, int rows, int cols)
        {
            // Make sure grid size have sane value
            cols = Math.Min(cols, 10);
            cols = Math.Max(cols, 1);
            rows = Math.Min(rows, 10);
            rows = Math.Max(rows, 1);

            // Get cell size, spacing & padding from the grid layout
            var gridLayoutGroup = gridContent.GetComponent<GridLayoutGroup>();
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
            var gridTransform = this.panelContainer.GetComponent<RectTransform>();
            gridTransform.sizeDelta = new Vector2(width, height);

            // Adjust grid container Y position to maintain constant height.
            // TODO: Figure out a way to adjust UI to avoid this calculation in code
            var gridPosition = new Vector3(gridTransform.anchoredPosition3D.x,
                (float)((gridTransform.rect.height - 2000) / 2.0),
                gridTransform.anchoredPosition3D.z);
            gridTransform.anchoredPosition3D = gridPosition;
        }

        public void StartPopulate()
        {
            // Ensure we only exeucte on populate routine at a time
            if (null != this.populateCoroutine)
            {
                StopCoroutine(this.populateCoroutine);
            }

            this.populateCoroutine = this.StartCoroutine(Populate());
        }

        /// <summary>
        /// Populate the grid from installed apps
        /// </summary>
        /// <returns></returns>
        private IEnumerator Populate()
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            Debug.Log("Persistent data path: " + persistentDataPath);

            // Load configuration
            Config config = new Config();
            ConfigPersistence.LoadConfig(config);

            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // Dictionary to hold package name -> app index, app name
                var packageNameToAppName = new Dictionary<string, (int Index, string TabName, string AppName)>();
                var excludedPackageNames = new HashSet<string>();
                var tabList = new List<string>();
                var isNoneTab = false;

                if (config.categoryType.Equals(Config.Category_None, StringComparison.OrdinalIgnoreCase))
                {
                    // If no categories, just create a placeholder tab
                    tabList.Add(Tab_None);
                    isNoneTab = true;
                }
                else
                {
                    // Create built-in categories
                    tabList.Add(Tab_Quest);
                    tabList.Add(Tab_Go);
                    tabList.Add(Tab_2D);
                }

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
                        // Skip excluded package
                        Debug.LogFormat("Skipping Excluded [{0}] Package: {1}, name: {2}", i, packageName, appName);
                        continue;
                    }

                    // Determine app type (None, Quest, Go or 2D)
                    string tabName;
                    if (isNoneTab)
                    {
                        tabName = Tab_None;
                    }
                    else if (currentActivity.Call<bool>("is2DApp", i))
                    {
                        if (!config.show2D)
                        {
                            // Skip 2D apps
                            Debug.LogFormat("Skipping 2D [{0}] Package: {1}, name: {2}", i, packageName, appName);
                            continue;
                        }

                        tabName = Tab_2D;
                    }
                    else if (currentActivity.Call<bool>("isQuestApp", i))
                    {
                        tabName = Tab_Quest;
                    }
                    else
                    {
                        tabName = Tab_Go;
                    }

                    packageNameToAppName.Add(packageName, (i, tabName, appName));
                    Debug.LogFormat("[{0}] package: {1}, name: {2}, tab: {3}", i, packageName, appName, tabName);
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
                            packageNameToAppName[entry[0]] = (packageNameToAppName[entry[0]].Index,
                                packageNameToAppName[entry[0]].TabName, entry[1]);
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

                // Populate the panel content
                yield return PopulatePanelContent(currentActivity, config, tabList, packageNameToAppName, iconOverrides);
            }
        }

        private IEnumerator PopulatePanelContent(
            AndroidJavaObject currentActivity,
            Config config,
            List<string> tabList,
            Dictionary<string, (int Index, string TabName, string AppName)> apps,
            Dictionary<string, string> iconOverrides)
        {
            // Destroy existing scrollviews and tabs
            for (int i = 0; i < this.tabContainer.transform.childCount; i++)
            {
                Destroy(this.tabContainer.transform.GetChild(i).gameObject);
                Destroy(this.scrollContainer.transform.GetChild(i).gameObject);
            }

            var gridContents = new Dictionary<string, (GameObject gridContent, GameObject scrollView, GameObject tab, bool isInUse)>();
            bool isFirstTab = true;

            // Create scroll views and tabs
            foreach (string tabName in tabList)
            {
                Debug.LogFormat("Populating tab '{0}'", tabName);

                // Create scroll view
                var scrollView = (GameObject)Instantiate(this.prefabScrollView, this.scrollContainer.transform);
                var scrollRectOverride = scrollView.GetComponent<ScrollRectOverride>();
                scrollRectOverride.trackingSpace = this.trackingSpace.transform;
                scrollRectOverride.name = tabName;

                var gridContent = scrollRectOverride.content.gameObject;
                scrollView.SetActive(isFirstTab);

                // Set grid size
                SetGridSize(gridContent, config.gridSize.rows, config.gridSize.cols);

                // Create tab
                var tab = (GameObject)Instantiate(this.prefabTab, this.tabContainer.transform);
                tab.GetComponentInChildren<TextMeshProUGUI>().text = tabName;

                var toggle = tab.GetComponent<Toggle>();
                toggle.isOn = isFirstTab;
                toggle.group = this.tabContainer.GetComponent<ToggleGroup>();
                toggle.onValueChanged.AddListener(scrollView.SetActive);

                if (config.categoryType.Equals(Config.Category_None, StringComparison.OrdinalIgnoreCase))
                {
                    // Hide the special "None" tab
                    tab.SetActive(false);
                }

                // Record the grid content
                gridContents[tabName] = (scrollView.GetComponent<ScrollRect>().content.gameObject,
                scrollView, tab, false);
                isFirstTab = false;
            }

            // Populate grid with app information (name & icon)
            // Sort by app name
            foreach (var app in apps.OrderBy(key => key.Value.AppName))
            {
                // Create new instances of our app info prefabCell
                var gridContent = gridContents[app.Value.TabName].gridContent;
                var newObj = (GameObject)Instantiate(this.prefabCell, gridContent.transform);

                // Mark that this grid content is in use
                gridContents[app.Value.TabName] = (gridContent, gridContents[app.Value.TabName].scrollView,
                    gridContents[app.Value.TabName].tab, true);

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

            // Remove any empty scroll views and tabs
            foreach (string tabName in tabList)
            {
                if (gridContents[tabName].isInUse)
                {
                    continue;
                }

                Debug.LogFormat("Removing empty tab '{0}'", tabName);
                Destroy(gridContents[tabName].scrollView);
                Destroy(gridContents[tabName].tab);
            }
        }
    }
    #endregion
}