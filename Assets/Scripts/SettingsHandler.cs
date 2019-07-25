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
        public GameObject gridContainer;
        public GameObject openSettingsButton;
        public GameObject closeSettingsButton;
        public GameObject settingsContainer;
        public GameObject gridCols;
        public GameObject gridColsText;
        public GameObject gridRows;
        public GameObject gridRowsText;
        public GameObject gridPopulation;

        private Config config = new Config();

        public void OpenSettings()
        {
            Debug.Log("Open Settings");
            this.gridContainer.SetActive(false);
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
        }

        public void CloseSettings()
        {
            // Resize grid if necessary
            ResizeGrid();

            Debug.Log("Close Settings");
            this.gridContainer.SetActive(true);
            this.openSettingsButton.SetActive(true);
            this.closeSettingsButton.SetActive(false);
            this.settingsContainer.SetActive(false);
        }

        public void DeleteExcludedApksFile()
        {
            Debug.Log("Delete Excluded Apk List");
            QuestAppLauncher.GridPopulation.DeleteExcludedApksFile();
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

        private void ResizeGrid()
        {
            var cols = (int)gridCols.GetComponent<Slider>().value;
            var rows = (int)gridRows.GetComponent<Slider>().value;

            if (cols == this.config.gridSize.cols &&
                rows == this.config.gridSize.rows)
            {
                // Nothing was resized, so no work to do
                return;
            }

            Debug.Log(string.Format("Resizing grid: {0} x {1}", cols, rows));

            // Update configuration
            this.config.gridSize.cols = cols;
            this.config.gridSize.rows = rows;
            ConfigPersistence.SaveConfig(this.config);

            // Update grid size
            this.gridPopulation.GetComponent<GridPopulation>().SetGridSize(this.config.gridSize.rows, this.config.gridSize.cols);
        }
    }
}