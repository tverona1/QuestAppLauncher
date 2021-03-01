using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuestAppLauncher
{
    public class RenameHandler : MonoBehaviour
    {
        // Referenced gameobjects
        public GameObject panelContainer;
        public GameObject closeRenameButton;
        public GameObject renamePanelContainer;
        public GameObject renameScrollContainer;
        public TextMeshProUGUI renameLabel;
        public GridPopulation gridPopulation;

        // Whether the rename grid has already been populated
        private bool isPopulated = false;

        // App entry of the app to rename
        private AppEntry appToRename;

        /// <summary>
        /// Opens the rename panel.
        /// </summary>
        /// <param name="appToRename">App to rename</param>
        public async void OpenRenamePanel(AppEntry appToRename)
        {
            Debug.Log("Open Rename Panel");
            this.panelContainer.SetActive(false);
            this.closeRenameButton.SetActive(true);
            this.renamePanelContainer.SetActive(true);

            this.appToRename = appToRename;

            this.renameLabel.text = string.Format("Pick entry to rename app '{0}' (package '{1}')", appToRename.appName, appToRename.packageId);

            // Only populate once.
            // This will be reset in the next scene load (by design, since we may download new assets).
            if (!this.isPopulated)
            {
                await gridPopulation.PopulateRenameAsync();
                this.isPopulated = true;
            }
        }

        /// <summary>
        /// Opens the rename dialog
        /// </summary>
        /// <param name="entry"></param>
        public async void Rename(AppEntry appTarget)
        {
            Debug.Assert(null != this.appToRename, "App to rename is null");
            var filePath = appTarget.externalIconPath;
            bool cleanupFile = false;

            // If icon path is null, extract icon from apk
            if (null == filePath)
            {
                filePath = Path.Combine(AssetsDownloader.GetOrCreateDownloadPath(), this.appToRename.packageId + ".jpg");
                cleanupFile = true;

                var bytes = appTarget.sprite.GetComponent<Image>().sprite.texture.EncodeToJPG();
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 4096, useAsync: true))
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                };
            }

            await Task.Run(() =>
            {
                AndroidJNI.AttachCurrentThread();

                try
                {
                    // Add to json file
                    AddToRenameJsonFile(this.appToRename.packageId, appTarget.appName);

                    // Add icon to zip
                    using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        var renameIconPackFilePath = Path.Combine(AppConfig.persistentDataPath, AppProcessor.RenameIconPackFileName);
                        currentActivity.CallStatic("addFileToZip", renameIconPackFilePath, filePath, this.appToRename.packageId + ".jpg");
                    }
                }
                finally
                {
                    AndroidJNI.DetachCurrentThread();
                }
            });

            if (cleanupFile && File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Reload the scene
            await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Closes the rename panel
        /// </summary>
        public void CloseRenamePanel()
        {
            Debug.Log("Close Rename Panel");
            this.panelContainer.SetActive(true);
            this.closeRenameButton.SetActive(false);
            this.renamePanelContainer.SetActive(false);
        }

        /// <summary>
        /// Adds app to rename to json file
        /// </summary>
        /// <param name="packageId">Package id of app</param>
        /// <param name="appName">App name</param>
        private void AddToRenameJsonFile(string packageId, string appName)
        {
            var renameJsonFilePath = Path.Combine(AppConfig.persistentDataPath, AppProcessor.RenameJsonFileName);
            Dictionary<string, AppProcessor.JsonAppNamesEntry> jsonAppNames = null;

            if (File.Exists(renameJsonFilePath))
            {
                // Read rename json file
                try
                {
                    var json = File.ReadAllText(renameJsonFilePath, Encoding.UTF8);
                    jsonAppNames = JsonConvert.DeserializeObject<Dictionary<string, AppProcessor.JsonAppNamesEntry>>(json);
                }
                catch (Exception e)
                {
                    // On exception, we'll keep going & just overwrite existing file contents
                    Debug.LogFormat("Failed to process json rename app file: {0}", e.Message);
                }
            }

            // Add entry
            if (null == jsonAppNames)
            {
                jsonAppNames = new Dictionary<string, AppProcessor.JsonAppNamesEntry>();
            }

            jsonAppNames[packageId] = new AppProcessor.JsonAppNamesEntry { Name = appName };

            // Persist
            try
            {
                File.WriteAllText(renameJsonFilePath, JsonConvert.SerializeObject(jsonAppNames, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Failed to write json rename app file: {0}", e.Message));
            }
        }
    }
}