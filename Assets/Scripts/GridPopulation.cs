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
        class ProcessedApp
        {
            public int Index;
            public string PackageName;
            public string AppName;
            public string AutoTabName;
            public string Tab1Name;
            public string Tab2Name;
            public bool IsOverride;
        }

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

        // Tab containers
        public GameObject topTabContainer;
        public GameObject leftTabContainer;
        public GameObject rightTabContainer;

        // Tab container
        public GameObject topTabContainerContent;
        public GameObject leftTabContainerContent;
        public GameObject rightTabContainerContent;

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
        private const string Tab_Quest = "Quest";
        private const string Tab_Go = "Go/Gear";
        private const string Tab_2D = "2D";
        private const string Tab_All = "All";

        private static readonly string[] Auto_Tabs = { Tab_Quest, Tab_Go, Tab_2D };

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
        /// <returns>true if file exists</returns>
        static public bool DeleteExcludedAppsFile()
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);

            if (File.Exists(excludedPackageNamesFilePath))
            {
                File.Delete(excludedPackageNamesFilePath);
                return true;
            }

            return false;
        }

        public Vector2 SetGridSize(GameObject gridContent, int rows, int cols)
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
            var size = new Vector2(width, height);

            var gridTransform = this.panelContainer.GetComponent<RectTransform>();
            gridTransform.sizeDelta = size;

            // Adjust grid container Y position to maintain constant height.
            // TODO: Figure out a way to adjust UI to avoid this calculation in code
            var gridPosition = new Vector3(gridTransform.anchoredPosition3D.x,
                (float)((gridTransform.rect.height - 2000) / 2.0),
                gridTransform.anchoredPosition3D.z);
            gridTransform.anchoredPosition3D = gridPosition;

            return size;
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
                var apps = new Dictionary<string, ProcessedApp>();
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
                        // Skip excluded package
                        Debug.LogFormat("Skipping Excluded [{0}] Package: {1}, name: {2}", i, packageName, appName);
                        continue;
                    }

                    // Determine app type (Quest, Go or 2D)
                    string tabName;
                    if (currentActivity.Call<bool>("is2DApp", i))
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

                    apps.Add(packageName, new ProcessedApp { PackageName = packageName, Index = i, AutoTabName = tabName, AppName = appName });
                    Debug.LogFormat("[{0}] package: {1}, name: {2}, auto tab: {3}", i, packageName, appName, tabName);
                    yield return null;
                }

                // Override app names, if any
                // This is just a file with comma-separated packageName,appName[,category1[, category2]]
                // Category1 and category2 are optional categories (tabs).
                var appNameOverrideFilePath = Path.Combine(persistentDataPath, AppNameOverrideFile);
                if (File.Exists(appNameOverrideFilePath))
                {
                    Debug.Log("Found file: " + appNameOverrideFilePath);
                    string[] lines = File.ReadAllLines(appNameOverrideFilePath);
                    foreach (string line in lines)
                    {
                        line.Trim();

                        if (line.StartsWith("#"))
                        {
                            // Skip comments
                            continue;
                        }

                        // Parse line
                        var entry = line.Split(',');
                        if (entry.Length < 2)
                        {
                            // We expect at least two entries
                            continue;
                        }

                        var packageName = entry[0];
                        var appName = entry[1];

                        if (!apps.ContainsKey(packageName))
                        {
                            // App is not installed, so skip
                            continue;
                        }

                        // Get the custom tab names, if any
                        string autoTabName = null;
                        var tab1 = entry.Length > 2 ? entry[2] : null;
                        var tab2 = entry.Length > 3 ? entry[3] : null;

                        if (tab1 != null && tab1.Length == 0)
                        {
                            tab1 = null;
                        }

                        if (tab2 != null && tab2.Length == 0)
                        {
                            tab2 = null;
                        }

                        if (tab1 != null && tab2 != null && tab1.Equals(tab2, StringComparison.OrdinalIgnoreCase))
                        {
                            tab2 = null;
                        }

                        // Override auto tabe name if custom name matches built-in tab name
                        if (tab1 != null && Auto_Tabs.Contains(tab1, StringComparer.OrdinalIgnoreCase))
                        {
                            autoTabName = tab1;
                            tab1 = null;
                        }

                        if (tab2 != null && Auto_Tabs.Contains(tab2, StringComparer.OrdinalIgnoreCase))
                        {
                            autoTabName = tab2;
                            tab2 = null;
                        }

                        // Update entry
                        apps[packageName] = new ProcessedApp
                        {
                            PackageName = apps[entry[0]].PackageName,
                            Index = apps[entry[0]].Index,
                            AppName = appName,
                            AutoTabName = autoTabName ?? apps[entry[0]].AutoTabName,
                            Tab1Name = tab1 ?? apps[entry[0]].Tab1Name,
                            Tab2Name = tab2 ?? apps[entry[0]].Tab2Name,
                            IsOverride = true
                        };
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
                        if (apps.ContainsKey(entry))
                        {
                            Debug.Log("Found file: " + iconFileName);
                            iconOverrides[entry] = Path.Combine(iconOverridePath, iconFileName);
                        }

                        yield return null;
                    }
                }

                yield return null;

                // Populate the panel content
                yield return PopulatePanelContent(currentActivity, config, apps, iconOverrides);
            }
        }

        private IEnumerator PopulatePanelContent(
            AndroidJavaObject currentActivity,
            Config config,
            Dictionary<string, ProcessedApp> apps,
            Dictionary<string, string> iconOverrides)
        {
            // Set up tabs
            var topTabs = new List<string>();
            var leftTabs = new List<string>();
            var rightTabs = new List<string>();

            // Set auto tabs
            if (config.autoCategory.Equals(Config.Category_Top, StringComparison.OrdinalIgnoreCase))
            {
                topTabs.AddRange(Auto_Tabs);
            }
            else if (config.autoCategory.Equals(Config.Category_Left, StringComparison.OrdinalIgnoreCase))
            {
                leftTabs.AddRange(Auto_Tabs);
            }
            else if (config.autoCategory.Equals(Config.Category_Right, StringComparison.OrdinalIgnoreCase))
            {
                rightTabs.AddRange(Auto_Tabs);
            }

            // Set custom tabs, sorted alphabetically
            var customTabs = apps.Where(x => null != x.Value.Tab1Name).Select(x => x.Value.Tab1Name).Union(apps
                .Where(x => null != x.Value.Tab2Name).Select(x => x.Value.Tab2Name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            customTabs.Sort();

            if (config.customCategory.Equals(Config.Category_Top, StringComparison.OrdinalIgnoreCase))
            {
                topTabs.AddRange(customTabs);
            }
            else if (config.customCategory.Equals(Config.Category_Left, StringComparison.OrdinalIgnoreCase))
            {
                leftTabs.AddRange(customTabs);
            }
            else if (config.customCategory.Equals(Config.Category_Right, StringComparison.OrdinalIgnoreCase))
            {
                rightTabs.AddRange(customTabs);
            }

            // Add the "all" top tab
            topTabs.Add(Tab_All);

            // Process the tab containers
            var gridContents = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            ProcessTabContainer(config, topTabs, this.topTabContainer, this.topTabContainerContent, true, gridContents);
            ProcessTabContainer(config, leftTabs, this.leftTabContainer, this.leftTabContainerContent, false, gridContents);
            ProcessTabContainer(config, rightTabs, this.rightTabContainer, this.rightTabContainerContent, false, gridContents);

            // Set panel size, use any grid content for reference (since they are all the same size)
            if (gridContents.Count > 0)
            {
                var size = SetGridSize(gridContents.First().Value, config.gridSize.rows, config.gridSize.cols);

                // Adjust tab sizes
                ResizeTabContent(this.topTabContainer.transform, size, topTabs.Count, true);
                ResizeTabContent(this.leftTabContainer.transform, size, leftTabs.Count, false);
                ResizeTabContent(this.rightTabContainer.transform, size, rightTabs.Count, false);
            }

            // Populate grid with app information (name & icon)
            // Sort by app name
            foreach (var app in apps.OrderBy(key => key.Value.AppName))
            {
                // Add to all tab
                yield return AddCellToGrid(app.Value, gridContents[Tab_All].transform, iconOverrides, currentActivity);

                // Add to auto (built-in) tabs
                if (gridContents.ContainsKey(app.Value.AutoTabName))
                {
                    yield return AddCellToGrid(app.Value, gridContents[app.Value.AutoTabName].transform, iconOverrides, currentActivity);
                }

                // Add to tab1
                if (null != app.Value.Tab1Name && gridContents.ContainsKey(app.Value.Tab1Name))
                {
                    yield return AddCellToGrid(app.Value, gridContents[app.Value.Tab1Name].transform, iconOverrides, currentActivity);
                }

                // Add to tab2
                if (null != app.Value.Tab2Name && gridContents.ContainsKey(app.Value.Tab2Name))
                {
                    yield return AddCellToGrid(app.Value, gridContents[app.Value.Tab2Name].transform, iconOverrides, currentActivity);
                }
            }
        }

        private void ResizeTabContent(Transform transform, Vector2 size, int childCount, bool isHorizontal)
        {
            var rect = transform.GetComponent<RectTransform>();

            // Resize the transform
            Vector2 newSize;
            if (isHorizontal)
            {
                newSize = new Vector2(size.x, rect.rect.height);
            }
            else
            {
                newSize = new Vector2(rect.rect.width, size.y);
            }

            rect.sizeDelta = newSize;

            // Resize box collider
            var boxCollider = transform.GetComponent<BoxCollider>();
            boxCollider.size = new Vector3(newSize.x, newSize.y, (float)0.05);

            // Refresh tab prev / next buttons
            transform.gameObject.GetComponent<ScrollButtonHandler>().RefreshScrollContent(childCount);
        }

        private void ProcessTabContainer(Config config, List<string> tabs, GameObject tabContainer,
            GameObject tabContainerContent, bool setFirstTabActive, Dictionary<string, GameObject> gridContents)
        {
            // Create scroll views and tabs
            foreach (string tabName in tabs)
            {
                Debug.LogFormat("Populating tab '{0}'", tabName);

                // Create scroll view
                var scrollView = (GameObject)Instantiate(this.prefabScrollView, this.scrollContainer.transform);
                var scrollRectOverride = scrollView.GetComponent<ScrollRectOverride>();
                scrollRectOverride.trackingSpace = this.trackingSpace.transform;

                var gridContent = scrollRectOverride.content.gameObject;
                scrollView.SetActive(setFirstTabActive);

                // Create tab
                var tab = (GameObject)Instantiate(this.prefabTab, tabContainerContent.transform);
                tab.GetComponentInChildren<TextMeshProUGUI>().text = tabName;

                var toggle = tab.GetComponent<Toggle>();
                toggle.isOn = setFirstTabActive;
                toggle.group = this.panelContainer.GetComponent<ToggleGroup>();
                toggle.onValueChanged.AddListener(scrollView.SetActive);

                setFirstTabActive = false;

                // Record the grid content
                gridContents[tabName] = scrollView.GetComponent<ScrollRect>().content.gameObject;
            }
        }

        private IEnumerator AddCellToGrid(ProcessedApp app, Transform transform,
            Dictionary<string, string> iconOverrides,
            AndroidJavaObject currentActivity)
        {
            // Create new instances of our app info prefabCell
            var newObj = (GameObject)Instantiate(this.prefabCell, transform);

            // Set app entry info
            var appEntry = newObj.GetComponent("AppEntry") as AppEntry;
            appEntry.packageId = app.PackageName;
            appEntry.appName = app.AppName;

            // Get app icon
            byte[] bytesIcon = null;
            bool useApkIcon = true;
            if (iconOverrides.ContainsKey(app.PackageName))
            {
                // Use overridden icon
                try
                {
                    bytesIcon = File.ReadAllBytes(iconOverrides[app.PackageName]);
                    useApkIcon = false;
                }
                catch (Exception e)
                {
                    // Fall back to using the apk icon
                    Debug.Log(string.Format("Error reading app icon from file [{0}]: {1}", iconOverrides[app.PackageName], e.Message));
                }
            }

            if (useApkIcon)
            {
                // Use built-in icon from the apk
                bytesIcon = (byte[])(Array)currentActivity.Call<sbyte[]>("getIcon", app.Index);
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
            text.text = app.AppName;

            yield return null;
        }
    }
    #endregion
}