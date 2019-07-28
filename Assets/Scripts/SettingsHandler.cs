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
        public GameObject showOnlyCustomToggle;

        public Toggle tabsNone;
        public Toggle tabsAuto;
        public Toggle tabsCustom;

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

            // Set ShowOnlyCustom toggle
            this.showOnlyCustomToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(this.config.showOnlyCustom);
            
            // Set tab mode
            if (this.config.categoryType.Equals(Config.Category_None, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsNone.isOn = true;
            }
            else if (this.config.categoryType.Equals(Config.Category_Auto, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsAuto.isOn = true;
            }
            else
            {
                this.tabsCustom.isOn = true;
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
                this.deletedHiddenAppsFile = QuestAppLauncher.GridPopulation.DeleteExcludedAppsFile();
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

            // Update ShowOnlyCustom toggle
            var showOnlyCustom = this.showOnlyCustomToggle.GetComponent<Toggle>().isOn;
            if (showOnlyCustom != this.config.showOnlyCustom)
            {
                this.config.showOnlyCustom = showOnlyCustom;
                saveConfig = true;
            }

            // Update tabbing
            string tabMode;
            if (this.tabsNone.isOn)
            {
                tabMode = Config.Category_None;
            }
            else if (this.tabsAuto.isOn)
            {
                tabMode = Config.Category_Auto;
            }
            else
            {
                tabMode = Config.Category_Custom;
            }

            if (!this.config.categoryType.Equals(tabMode, StringComparison.OrdinalIgnoreCase))
            {
                this.tabsNone.isOn = true;
                this.config.categoryType = tabMode;
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
                this.gridPopulation.GetComponent<GridPopulation>().StartPopulate();
            }
        }
    }
}