using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OnInteraction : MonoBehaviour
{
    protected Material oldHoverMat;
    public Material yellowMat;
    public Material backIdle;
    public Material backACtive;
    public UnityEngine.UI.Text outText;

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

    public void OnSelected(Transform t)
    {
        var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
        if (null != appEntry)
        {
            // Launch app
            Debug.Log("Launching: " + appEntry.appName + " (package id: " + appEntry.packageId + ")");
            QuestAppLauncher.GridPopulation.LaunchApp(appEntry.packageId);
        }
    }

    public void OnSelectedPressedBorY(Transform t)
    {
        var appEntry = t.gameObject.GetComponent("AppEntry") as AppEntry;
        if (null != appEntry)
        {
            // Add package name to excluded file
            Debug.Log("Hiding: " + appEntry.appName + " (package id: " + appEntry.packageId + ")");
            QuestAppLauncher.GridPopulation.AddAppToExcludedFile(appEntry.packageId);

            // Remove ourselves from the gridview
            Destroy(t.gameObject);
        }
    }

    void EnableBorder(Transform t, bool enable)
    {
        var border = t.Find("Border");
        border?.gameObject.SetActive(enable);
    }
}
