using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestAppLauncher
{
    public class HideAppHandler : MonoBehaviour
    {
        public TextMeshProUGUI text;
        private AppEntry appEntryToHide;

        public void OnHideApp(AppEntry appEntryToHide)
        {
            this.appEntryToHide = appEntryToHide;

            // Set dialog text
            this.text.text = string.Format("Hide app '{0}'?", appEntryToHide.appName);

            // Launch dialog
            this.gameObject.SetActive(true);
        }

        public async void OnOk()
        {
            // Add package name to excluded file
            Debug.Log("Hiding: " + this.appEntryToHide.appName + " (package id: " + this.appEntryToHide.packageId + ")");
            AppProcessor.AddAppToExcludedFile(this.appEntryToHide.packageId);

            // Reload the scene - this will force all the tabs to update
            await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }

        public void OnCancel()
        {
            this.gameObject.SetActive(false);
        }
    }
}