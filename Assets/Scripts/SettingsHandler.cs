using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsHandler : MonoBehaviour
{
    public GameObject scrollView;
    public GameObject openSettingsButton;
    public GameObject closeSettingsButton;
    public GameObject settingsView;

    public void OpenSettings()
    {
        Debug.Log("Open Settings");
        scrollView.SetActive(false);
        openSettingsButton.SetActive(false);
        closeSettingsButton.SetActive(true);
        settingsView.SetActive(true);
    }

    public void CloseSettings()
    {
        Debug.Log("Close Settings");
        scrollView.SetActive(true);
        openSettingsButton.SetActive(true);
        closeSettingsButton.SetActive(false);
        settingsView.SetActive(false);
    }

    public void RefreshScene()
    {
        Debug.Log("Scene refreshed");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void DeleteExcludedApksFile()
    {
        Debug.Log("Delete Excluded Apk List");
        QuestAppLauncher.GridPopulation.DeleteExcludedApksFile();
    }
}
