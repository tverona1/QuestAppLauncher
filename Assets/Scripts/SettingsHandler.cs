using System;
using System.Collections;
using System.Collections.Generic;
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

        public Toggle tabsAutoOff;
        public Toggle tabsAutoTop;
        public Toggle tabsAutoLeft;
        public Toggle tabsAutoRight;

        public Toggle tabsCustomOff;
        public Toggle tabsCustomTop;
        public Toggle tabsCustomLeft;
        public Toggle tabsCustomRight;

        private bool deletedHiddenAppsFile = false;

        private Config config = new Config();

        public void OpenSettings()
        {
            Debug.Log("Open Settings");
            this.panelContainer.SetActive(false);
            this.openSettingsButton.SetActive(false);
            this.closeSettingsButton.SetActive(true);
            this.settingsContainer.SetActive(true);

            // Load config
            ConfigPersistence.LoadConfig(this.config);

            // Set current cols & rows
            var colsSlider = this.gridCols.GetComponent<Slider>();
            colsSlider.value = this.config.gridSize.cols;
            var colsText = this.gridColsText.GetComponent<TextMeshProUGUI>();
            colsText.text = string.Format("{0} Cols", this.config.gridSize.cols);

            var rowsSlider = this.gridRows.GetComponent<Slider>();
            rowsSlider.value = this.config.gridSize.rows;

            var rowsText = this.gridRowsText.GetComponent<TextMeshProUGUI>();
            rowsText.text = string.Format("{0} Rows", this.config.gridSize.rows);

            // Set 2D toggle
            this.show2DToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(this.config.show2D);

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

        public void DeleteExcludedApksFile()
        {
            Debug.Log("Delete Excluded App List");

            if (!this.deletedHiddenAppsFile)
            {
                this.deletedHiddenAppsFile = AppProcessor.DeleteExcludedAppsFile();
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

        private void PersistConfig()
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

            // Update 2D toggle
            var show2D = this.show2DToggle.GetComponent<Toggle>().isOn;
            if (show2D != this.config.show2D)
            {
                this.config.show2D = show2D;
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
            if (saveConfig || deletedHiddenAppsFile)
            {
                Debug.Log("Re-populating panel");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}