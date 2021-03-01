using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace QuestAppLauncher
{
    public class SettingsHandler : MonoBehaviour
    {
        public GameObject panelContainer;
        public GameObject openSettingsButton;
        public GameObject closeSettingsButton;
        public GameObject settingsContainer;
        public GameObject gridCols;
        public GameObject gridColsText;
        public GameObject gridRows;
        public GameObject gridRowsText;
        public GameObject gridPopulation;
        public GameObject show2DToggle;
        public GameObject autoUpdateToggle;
        public GameObject skyBoxButton;
        public GameObject environmentButton;
        public DownloadStatusIndicator downloadStatusIndicator;
        public SkyboxHandler skyboxHandler;
        public EnvironmentHandler environmentHandler;
        public TextMeshProUGUI versionText;

        public Toggle tabsAutoOff;
        public Toggle tabsAutoTop;
        public Toggle tabsAutoLeft;
        public Toggle tabsAutoRight;

        public Toggle tabsCustomOff;
        public Toggle tabsCustomTop;
        public Toggle tabsCustomLeft;
        public Toggle tabsCustomRight;

        public Toggle sortAZ;
        public Toggle sortMostRecent;

        public GameObject usageStatsPermText;

        private bool deletedHiddenAppsFile = false;
        private bool deletedRenameFiles = false;

        private Config config = null;

        public void OpenSettings()
        {
            Debug.Log("Open Settings");
            this.panelContainer.SetActive(false);
            this.openSettingsButton.SetActive(false);
            this.closeSettingsButton.SetActive(true);
            this.settingsContainer.SetActive(true);

            this.deletedHiddenAppsFile = false;
            this.deletedRenameFiles = false;

            // Load config
            this.config = ConfigPersistence.LoadConfig();

            // Skybox callback
            this.skyboxHandler.OnSkyboxSelected = OnSkyboxSelected;

            // Environment callback
            this.environmentHandler.OnEnvironmentSelected = OnEnvironmentSelected;

            // Set version text
            this.versionText.text = string.Format("Version: {0}", Application.version);

            // Set current cols & rows
            var colsSlider = this.gridCols.GetComponent<Slider>();
            colsSlider.value = this.config.gridSize.cols;
            var colsText = this.gridColsText.GetComponent<TextMeshProUGUI>();
            colsText.text = string.Format("{0} Cols", this.config.gridSize.cols);

            var rowsSlider = this.gridRows.GetComponent<Slider>();
            rowsSlider.value = this.config.gridSize.rows;

            var rowsText = this.gridRowsText.GetComponent<TextMeshProUGUI>();
            rowsText.text = string.Format("{0} Rows", this.config.gridSize.rows);

            // initialize sort mode
            InitializeSortMode();

            // Set 2D toggle
            this.show2DToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(this.config.show2D);

            // Set skybox button text
            this.skyBoxButton.GetComponentInChildren<TextMeshProUGUI>().text = SkyboxHandler.GetSkyboxNameFromPath(this.config.background);

            // Set environment button text
            this.environmentButton.GetComponentInChildren<TextMeshProUGUI>().text = EnvironmentHandler.GetEnvironmentNameFromPath(this.config.environment);

            // Set auto-update toggle
            this.autoUpdateToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(this.config.autoUpdate);

            // Set auto tab mode
            if (this.config.autoCategory.Equals(Config.Category_Top, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsAutoTop.isOn = true;
            }
            else if (this.config.autoCategory.Equals(Config.Category_Left, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsAutoLeft.isOn = true;
            }
            else if (this.config.autoCategory.Equals(Config.Category_Right, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsAutoRight.isOn = true;
            }
            else
            {
                this.tabsAutoOff.isOn = true;
            }

            // Set custom tab mode
            if (this.config.customCategory.Equals(Config.Category_Top, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsCustomTop.isOn = true;
            }
            else if (this.config.customCategory.Equals(Config.Category_Left, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsCustomLeft.isOn = true;
            }
            else if (this.config.customCategory.Equals(Config.Category_Right, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsCustomRight.isOn = true;
            }
            else
            {
                this.tabsCustomOff.isOn = true;
            }
        }

        public void CloseSettings()
        {
            // Persist any config changes
            PersistConfig();

            Debug.Log("Close Settings");
            this.panelContainer.SetActive(true);
            this.openSettingsButton.SetActive(true);
            this.closeSettingsButton.SetActive(false);
            this.settingsContainer.SetActive(false);
        }

        public void UpdateAssetsNow()
        {
            AssetsDownloader.DownloadAssetsAsync(config, this.downloadStatusIndicator, true);
        }

        public void DeleteExcludedApksFile()
        {
            Debug.Log("Delete Excluded App List");

            if (!this.deletedHiddenAppsFile)
            {
                this.deletedHiddenAppsFile = AppProcessor.DeleteExcludedAppsFile();
            }
        }

        public void OnSkyboxSelected(string skyboxPath)
        {
            if (!EnvironmentHandler.IsNoneEnvironment(this.config.environment))
            {
                // Clear environment if selected
                this.environmentHandler.ClearEnvironment();
                SaveEnvironmentSelection(Config.Environment_None);
            }

            SaveSkyboxSelection(skyboxPath);
        }

        private void SaveSkyboxSelection(string skyboxPath)
        {
            // Update text
            this.skyBoxButton.GetComponentInChildren<TextMeshProUGUI>().text = SkyboxHandler.GetSkyboxNameFromPath(skyboxPath);

            // Save config with new skybox selection
            if (!this.config.background.Equals(skyboxPath, StringComparison.OrdinalIgnoreCase))
            {
                this.config.background = skyboxPath;
                ConfigPersistence.SaveConfig(this.config);
            }
        }

        public void OnEnvironmentSelected(string environmentPath)
        {
            // If no environment selected, load up the skybox. Otherwise, clear the skybox
            if (EnvironmentHandler.IsNoneEnvironment(environmentPath))
            {
                this.skyboxHandler.SetSkybox(this.config.background);
            }
            else
            {
                // Reset skybox selection to default
                this.skyboxHandler.ClearSkybox();
                SaveSkyboxSelection(Config.Background_Default);
            }

            SaveEnvironmentSelection(environmentPath);
        }

        private void SaveEnvironmentSelection(string environmentPath)
        {
            // Update text
            this.environmentButton.GetComponentInChildren<TextMeshProUGUI>().text = EnvironmentHandler.GetEnvironmentNameFromPath(environmentPath);

            // Save config with new skybox selection
            if (!this.config.environment.Equals(environmentPath, StringComparison.OrdinalIgnoreCase))
            {
                this.config.environment = environmentPath;
                ConfigPersistence.SaveConfig(this.config);
            }
        }

        public void DeleteRenameFiles()
        {
            Debug.Log("Delete Rename files");

            if (!this.deletedRenameFiles)
            {
                this.deletedRenameFiles = AppProcessor.DeleteRenameFiles();
            }
        }

        public void UpdateGridColText()
        {
            var cols = gridCols.GetComponent<Slider>().value;
            var colsText = this.gridColsText.GetComponent<TextMeshProUGUI>();
            colsText.text = string.Format("{0} Cols", cols);
        }

        public void UpdateGridRowText()
        {
            var rows = gridRows.GetComponent<Slider>().value;
            var rowsText = this.gridRowsText.GetComponent<TextMeshProUGUI>();
            rowsText.text = string.Format("{0} Rows", rows);
        }

        public void ShowSkyboxList()
        {
            this.skyboxHandler.ShowList();
        }

        public void ShowEnvironmentList()
        {
            this.environmentHandler.ShowList();
        }

        private bool HasUsageStatsPermissions()
        {
            // Check if we have UsageStats permission
            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var hasUsageStatsPermissions = currentActivity.Call<bool>("hasUsageStatsPermissions");
                Debug.LogFormat("UsageStatsPermission: {0}", hasUsageStatsPermissions);
                return hasUsageStatsPermissions;
            }
        }

        private void InitializeSortMode()
        {
            bool hasUsageStatsPermissions = HasUsageStatsPermissions();

            // Indicate whether we need to get permission
            this.usageStatsPermText.SetActive(!hasUsageStatsPermissions);

            if (hasUsageStatsPermissions &&
                this.config.sortMode.Equals(Config.Sort_MostRecent, StringComparison.OrdinalIgnoreCase))
            {
                // Have UsageStats permission, so set it to on
                this.sortMostRecent.isOn = true;
            }
            else
            {
                // Default is to sort by AZ
                this.sortAZ.isOn = true;
            }

            this.sortMostRecent.onValueChanged.AddListener((bool isOn) => {
                if (isOn)
                {
                    // Re-check permissions
                    bool hasPerms = HasUsageStatsPermissions();

                    // Indicate whether we need to get permission
                    this.usageStatsPermText.SetActive(!hasPerms);

                    if (!hasPerms)
                    {
                        this.sortMostRecent.SetIsOnWithoutNotify(false);
                        this.sortAZ.SetIsOnWithoutNotify(true);

                        // Ask for permissions
                        using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                        {
                            currentActivity.Call("grantUsageStatsPermission");

                            // Quest doesn't like multiple apps running - kill ourself
                            UnityEngine.Application.Quit();
                        }
                    }
                }
            });
        }

        private async void PersistConfig()
        {
            bool saveConfig = false;

            // Update grid size
            var cols = (int)gridCols.GetComponent<Slider>().value;
            var rows = (int)gridRows.GetComponent<Slider>().value;

            if (cols != this.config.gridSize.cols ||
                rows != this.config.gridSize.rows)
            {
                this.config.gridSize.cols = cols;
                this.config.gridSize.rows = rows;
                saveConfig = true;
            }

            // Update sort mode
            string sortMode;
            if (this.sortMostRecent.isOn)
            {
                sortMode = Config.Sort_MostRecent;
            }
            else
            {
                sortMode = Config.Sort_AZ;
            }

            if (!this.config.sortMode.Equals(sortMode, StringComparison.OrdinalIgnoreCase))
            {
                this.config.sortMode = sortMode;
                saveConfig = true;
            }

            // Update 2D toggle
            var show2D = this.show2DToggle.GetComponent<Toggle>().isOn;
            if (show2D != this.config.show2D)
            {
                this.config.show2D = show2D;
                saveConfig = true;
            }

            // Update auto-update toggle
            var autoUpdate = this.autoUpdateToggle.GetComponent<Toggle>().isOn;
            if (autoUpdate != this.config.autoUpdate)
            {
                this.config.autoUpdate = autoUpdate;
                saveConfig = true;
            }

            // Update auto tab mode
            string tabAutoMode;
            if (this.tabsAutoTop.isOn)
            {
                tabAutoMode = Config.Category_Top;
            }
            else if (this.tabsAutoLeft.isOn)
            {
                tabAutoMode = Config.Category_Left;
            }
            else if (this.tabsAutoRight.isOn)
            {
                tabAutoMode = Config.Category_Right;
            }
            else
            {
                tabAutoMode = Config.Category_Off;
            }

            if (!this.config.autoCategory.Equals(tabAutoMode, StringComparison.OrdinalIgnoreCase))
            {
                this.config.autoCategory = tabAutoMode;
                saveConfig = true;
            }

            // Update auto tab mode
            string tabCustomMode;
            if (this.tabsCustomTop.isOn)
            {
                tabCustomMode = Config.Category_Top;
            }
            else if (this.tabsCustomLeft.isOn)
            {
                tabCustomMode = Config.Category_Left;
            }
            else if (this.tabsCustomRight.isOn)
            {
                tabCustomMode = Config.Category_Right;
            }
            else
            {
                tabCustomMode = Config.Category_Off;
            }

            if (!this.config.customCategory.Equals(tabCustomMode, StringComparison.OrdinalIgnoreCase))
            {
                this.config.customCategory = tabCustomMode;
                saveConfig = true;
            }

            // Persist configuration & re-populate
            if (saveConfig)
            {
                ConfigPersistence.SaveConfig(this.config);
            }

            // If we touched the config file or we deleted the hidden apps file, re-populate the grid
            if (saveConfig || this.deletedHiddenAppsFile || this.deletedRenameFiles)
            {
                Debug.Log("Re-populating panel");
                await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            }
        }
    }
}