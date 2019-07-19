using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonHandler : MonoBehaviour
{
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
