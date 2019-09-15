using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace QuestAppLauncher
{
    public class OnInteraction : MonoBehaviour
    {
        // Hide app handler
        public HideAppHandler hideAppHandler;

        // Rename app handler
        public RenameHandler renameHandler;

        public void OnHoverEnter(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
            if (null != appEntry)
            {
                // Enable border
                EnableBorder(t, true);
            }
        }

        public void OnHoverExit(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
            if (null != appEntry)
            {
                // Disable border
                EnableBorder(t, false);
            }
        }

        public async void OnSelected(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
            if (null != appEntry)
            {
                if (appEntry.isRenameMode)
                {
                    this.renameHandler.Rename(appEntry);
                }
                else
                {
                    await Task.Run(() =>
                    {
                        AndroidJNI.AttachCurrentThread();

                        try
                        {
                            // Launch app
                            Debug.Log("Launching: " + appEntry.appName + " (package id: " + appEntry.packageId + ")");
                            AppProcessor.LaunchApp(appEntry.packageId);
                        }
                        finally
                        {
                            AndroidJNI.DetachCurrentThread();
                        }
                    });
                }
            }
        }

        public void OnSelectedPressedBorY(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
            if (null != appEntry && !appEntry.isRenameMode)
            {
                this.hideAppHandler.OnHideApp(appEntry);
            }
        }

        public void OnSelectedPressedAorX(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
            if (null != appEntry && !appEntry.isRenameMode)
            {
                this.renameHandler.OpenRenamePanel(appEntry);
            }
        }

        void EnableBorder(Transform t, bool enable)
        {
            var border = t.Find("Border");
            border?.gameObject.SetActive(enable);
        }
    }
}