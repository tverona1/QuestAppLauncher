using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadByName(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
}
