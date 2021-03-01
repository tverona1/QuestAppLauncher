using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public class AppComparer : IComparer<ProcessedApp>
        {
            public int Compare(ProcessedApp x, ProcessedApp y)
            {
                // Order by last used and then alphabetical to break ties
                return (x.LastTimeUsed != y.LastTimeUsed) ?
                    (y.LastTimeUsed - x.LastTimeUsed > 0 ? 1 : -1) :
                    string.Compare(x.AppName, y.AppName, true);
            }
        }

        // Max icons to initially populate. If we have more than this number,
        // then we fall back into dynamically loading icons as we scroll to conserve memory.
        const int MaxIconsOnInitialPopulation = 150;


        // Grid container game object
        public GameObject panelContainer;

        // Scroll container game object
        public GameObject scrollContainer;

        // SKybox handler
        public SkyboxHandler skyboxHandler;

        // Environment handler
        public EnvironmentHandler environmentHandler;

        // Tab containers
        public GameObject topTabContainer;
        public GameObject leftTabContainer;
        public GameObject rightTabContainer;

        // Tab container
        public GameObject topTabContainerContent;
        public GameObject leftTabContainerContent;
        public GameObject rightTabContainerContent;

        // Rename panel game objects
        public GameObject renamePanelContainer;
        public GameObject scrollRenameContainer;

        // Tracking space
        public GameObject trackingSpace;

        // Download status indicator
        public DownloadStatusIndicator downloadStatusIndicator;

        // App info prefab (a cell in the grid content)
        public GameObject prefabCell;

        // Scroll view prefab
        public GameObject prefabScrollView;

        // Tab prefab
        public GameObject prefabTab;

        #region MonoBehaviour handler

        async void Start()
        {
            // Set high texture resolution scale to minimize aliasing
            XRSettings.eyeTextureResolutionScale = 2.0f;

            // Initialize the core platform
            Core.AsyncInitialize();

            // Initialize app config on main thread
            AppConfig.Initialize();

            // Populate the grid
            await PopulateAsync();
        }

        void Update()
        {
        }
        #endregion

        /// <summary>
        /// Populates rename grid
        /// </summary>
        /// <returns></returns>
        public async Task PopulateRenameAsync()
        {
            PopulateAsync(true);
        }

        #region Private Functions
        /// <summary>
        /// Populate the grid from installed apps
        /// </summary>
        /// <returns></returns>
        private async Task PopulateAsync(bool isRenameMode = false)
        {
            // Load configuration
            var config = ConfigPersistence.LoadConfig();

            if (!isRenameMode)
            {
                // Set environment if selected; otherwise, set the skybox
                if (!EnvironmentHandler.IsNoneEnvironment(config.environment))
                {
                    await this.environmentHandler.SetEnvironment(config.environment);
                }
                else
                {
                    this.skyboxHandler.SetSkybox(config.background);
                }
            }

            // Process apps in background
            var apps = await Task.Run(() =>
            {
                AndroidJNI.AttachCurrentThread();

                try
                {
                    return AppProcessor.ProcessApps(config, isRenameMode);
                }
                finally
                {
                    AndroidJNI.DetachCurrentThread();
                }
            });

            // Download updates in the background
            if (!isRenameMode && config.autoUpdate && !GlobalState.Instance.CheckedForUpdate)
            {
                GlobalState.Instance.CheckedForUpdate = true;
                AssetsDownloader.DownloadAssetsAsync(config, this.downloadStatusIndicator);
            }

            // Populate the panel content
            if (!isRenameMode)
            {
                await PopulatePanelContentAsync(config, apps);
            }
            else
            {
                await PopulateRenamePanelContentAsync(config, apps);
            }
        }

        private async Task PopulatePanelContentAsync(
            Config config,
            Dictionary<string, ProcessedApp> apps)
        {
            // Set up tabs
            var topTabs = new List<string>();
            var leftTabs = new List<string>();
            var rightTabs = new List<string>();

            // Set auto tabs
            var autoTabs = AppProcessor.Auto_Tabs.Intersect(
                apps.Where(x => null != x.Value.AutoTabName).Select(x => x.Value.AutoTabName)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase).ToList(),
                StringComparer.CurrentCultureIgnoreCase);

            if (config.autoCategory.Equals(Config.Category_Top, StringComparison.OrdinalIgnoreCase))
            {
                topTabs.AddRange(autoTabs);
            }
            else if (config.autoCategory.Equals(Config.Category_Left, StringComparison.OrdinalIgnoreCase))
            {
                leftTabs.AddRange(autoTabs);
            }
            else if (config.autoCategory.Equals(Config.Category_Right, StringComparison.OrdinalIgnoreCase))
            {
                rightTabs.AddRange(autoTabs);
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
            topTabs.Add(AppProcessor.Tab_All);

            // Process the tab containers
            var gridContents = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            ProcessTabContainer(config, topTabs, this.panelContainer, this.scrollContainer, this.topTabContainer, this.topTabContainerContent, true, gridContents);
            ProcessTabContainer(config, leftTabs, this.panelContainer, this.scrollContainer, this.leftTabContainer, this.leftTabContainerContent, false, gridContents);
            ProcessTabContainer(config, rightTabs, this.panelContainer, this.scrollContainer, this.rightTabContainer, this.rightTabContainerContent, false, gridContents);

            // Set panel size, use any grid content for reference (since they are all the same size)
            if (gridContents.Count > 0)
            {
                var size = SetGridSize(this.panelContainer, gridContents.First().Value, config.gridSize.rows, config.gridSize.cols);

                // Adjust tab sizes
                ResizeTabContent(this.topTabContainer.transform, size, topTabs.Count, true);
                ResizeTabContent(this.leftTabContainer.transform, size, leftTabs.Count, false);
                ResizeTabContent(this.rightTabContainer.transform, size, rightTabs.Count, false);
            }

            bool loadIcons = apps.Count <= MaxIconsOnInitialPopulation;

            // Populate grid with app information (name & icon)
            // Sort by custom comparer
            foreach (var app in apps.OrderBy(key => key.Value, new AppComparer()))
            {
                // Add to all tab
                await AddCellToGridAsync(app.Value, gridContents[AppProcessor.Tab_All].transform, loadIcons);

                // Add to auto (built-in) tabs
                if (gridContents.ContainsKey(app.Value.AutoTabName))
                {
                    await AddCellToGridAsync(app.Value, gridContents[app.Value.AutoTabName].transform, loadIcons);
                }

                // Add to tab1
                if (null != app.Value.Tab1Name && gridContents.ContainsKey(app.Value.Tab1Name))
                {
                    await AddCellToGridAsync(app.Value, gridContents[app.Value.Tab1Name].transform, loadIcons);
                }

                // Add to tab2
                if (null != app.Value.Tab2Name && gridContents.ContainsKey(app.Value.Tab2Name))
                {
                    await AddCellToGridAsync(app.Value, gridContents[app.Value.Tab2Name].transform, loadIcons);
                }
            }
        }

        private async Task PopulateRenamePanelContentAsync(
            Config config, Dictionary<string, ProcessedApp> apps)
        {
            // Create scroll view
            var scrollView = (GameObject)Instantiate(this.prefabScrollView, this.scrollRenameContainer.transform);
            scrollView.SetActive(true);
            var scrollRectOverride = scrollView.GetComponent<ScrollRectOverride>();
            scrollRectOverride.trackingSpace = this.trackingSpace.transform;

            var gridContent = scrollRectOverride.content.gameObject;

            // Set panel size
            SetGridSize(this.renamePanelContainer, gridContent, config.gridSize.rows, config.gridSize.cols);

            bool loadIcons = apps.Count <= MaxIconsOnInitialPopulation;

            // Populate grid with app information (name & icon)
            // Sort alphabetically
            foreach (var app in apps.OrderBy(key => key.Value.AppName, StringComparer.InvariantCultureIgnoreCase))
            {
                await AddCellToGridAsync(app.Value, gridContent.transform, loadIcons, true);
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

        private Vector2 SetGridSize(GameObject panel, GameObject gridContent, int rows, int cols)
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

            var gridTransform = panel.GetComponent<RectTransform>();
            gridTransform.sizeDelta = size;

            // Adjust grid container Y position to maintain constant height.
            // TODO: Figure out a way to adjust UI to avoid this calculation in code
            var gridPosition = new Vector3(gridTransform.anchoredPosition3D.x,
                (float)((gridTransform.rect.height - 2000) / 2.0),
                gridTransform.anchoredPosition3D.z);
            gridTransform.anchoredPosition3D = gridPosition;

            return size;
        }

        private void ProcessTabContainer(Config config, List<string> tabs, GameObject panel, GameObject scroll, GameObject tabContainer,
            GameObject tabContainerContent, bool setFirstTabActive, Dictionary<string, GameObject> gridContents)
        {
            // Create scroll views and tabs
            foreach (string tabName in tabs)
            {
                Debug.LogFormat("Populating tab '{0}'", tabName);

                // Create scroll view
                var scrollView = (GameObject)Instantiate(this.prefabScrollView, scroll.transform);
                var scrollRectOverride = scrollView.GetComponent<ScrollRectOverride>();
                scrollRectOverride.trackingSpace = this.trackingSpace.transform;

                var gridContent = scrollRectOverride.content.gameObject;
                scrollView.SetActive(setFirstTabActive);

                // Create tab
                var tab = (GameObject)Instantiate(this.prefabTab, tabContainerContent.transform);
                tab.GetComponentInChildren<TextMeshProUGUI>().text = tabName;

                var toggle = tab.GetComponent<Toggle>();
                toggle.isOn = setFirstTabActive;
                toggle.group = panel.GetComponent<ToggleGroup>();
                toggle.onValueChanged.AddListener(scrollView.SetActive);

                setFirstTabActive = false;

                // Record the grid content
                gridContents[tabName] = scrollView.GetComponent<ScrollRect>().content.gameObject;
            }
        }

        private async Task AddCellToGridAsync(ProcessedApp app, Transform transform, bool loadIcon, bool isRenameMode = false)
        {
            if (app.Index == -1 && string.IsNullOrEmpty(app.IconPath))
            {
                // If we have neither app index or icon path, skip this
                return;
            }

            // Create new instances of our app info prefabCell
            var newObj = (GameObject)Instantiate(this.prefabCell, transform);

            // Set app entry info
            var appEntry = newObj.GetComponent<AppEntry>();
            appEntry.scrollViewTransform = transform.parent.parent.gameObject.GetComponent<RectTransform>();
            appEntry.packageId = app.PackageName;
            appEntry.appName = app.AppName;
            appEntry.isRenameMode = isRenameMode;
            appEntry.installedApkIndex = app.Index;
            appEntry.externalIconPath = app.IconPath;

            // Dynamically load icon if we're not loading the icon now
            appEntry.dynamicallyLoadIcon = !loadIcon;

            // Set the icon image
            if (loadIcon)
            {
                await appEntry.LoadIcon();
            }

            // Set app name in text
            appEntry.text.text = app.AppName;
        }
    }
    #endregion
}