#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
 
public class NullComponentAndMissingPrefabSearchTool
{
  [MenuItem("Tools/Log Missing Prefabs And Components")]
  public static void Search()
  {
    results.Clear();
    GameObject[] gos = SceneManager.GetActiveScene().GetRootGameObjects();
    foreach (GameObject go in gos) Traverse(go.transform);
    Debug.Log("> Total Results: " + results.Count);
    foreach (string result in results) Debug.Log("> " + result);
  }
 
  private static List<string> results = new List<string>();
  private static void AppendComponentResult(string childPath, int index)
  {
    results.Add("Missing Component " + index + " of " + childPath);
  }
  private static void AppendTransformResult(string childPath, string name)
  {
    results.Add("Missing Prefab for \"" + name + "\" of " + childPath);
  }
  private static void Traverse(Transform transform, string path = "")
  {
    string thisPath = path + "/" + transform.name;
    Component[] components = transform.GetComponents<Component>();
    for (int i = 0; i < components.Length; i++)
    {
      if (components[i] == null) AppendComponentResult(thisPath, i);
    }
    for (int c = 0; c < transform.childCount; c++)
    {
      Transform t = transform.GetChild(c);
      PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(t.gameObject);
      if (pt == PrefabAssetType.MissingAsset)
      {
        AppendTransformResult(path + "/" + transform.name, t.name);
      } else
      {
        Traverse(t, thisPath);
      }
    }
  }
}
#endif