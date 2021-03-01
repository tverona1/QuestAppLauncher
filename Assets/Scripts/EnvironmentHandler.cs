using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace QuestAppLauncher
{
    public class EnvironmentHandler : MonoBehaviour
    {
        // Environment selected callback
        public Action<string> OnEnvironmentSelected;

        // Environment parent container
        public GameObject envParentContainer;

        // Environment List Container
        public GameObject envListContainer;

        // Environment list entry prefab
        public GameObject prefabEnvironmentEntry;

        // Content transform
        public Transform contentTransform;

        // Environment folder
        private const string EnvironmentFolder = "environments";

        // Extension search
        const string PrefabExtSearch = "*.prefab";

        /// <summary>
        /// Show the environment list dialog
        /// </summary>
        public async void ShowList()
        {
            // Show the dialog
            this.envListContainer.SetActive(true);

            // Populate the list
            await PopulateAsync();
        }

        public void OnCancel()
        {
            // Hide the dialog
            this.envListContainer.SetActive(false);
        }

        public void OnHoverEnter(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("EnvironmentEntry") as EnvironmentEntry;
            if (null != appEntry)
            {
                // Enable border
                EnableBorder(t, true);
            }
        }

        public void OnHoverExit(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("EnvironmentEntry") as EnvironmentEntry;
            if (null != appEntry)
            {
                // Disable border
                EnableBorder(t, false);
            }
        }

        public async void OnSelected(Transform t)
        {
            var entry = t.gameObject.GetComponent("EnvironmentEntry") as EnvironmentEntry;
            if (null != entry)
            {
                // Set the environment
                await SetEnvironment(entry.path);
                this.envListContainer.SetActive(false);

                // Callback if registered
                if (null != OnEnvironmentSelected)
                {
                    OnEnvironmentSelected(entry.path);
                }
            }
        }

        /// <summary>
        /// Return environment name given its path (i.e. filename w/o extension)
        /// </summary>
        /// <param name="path">Path to environments</param>
        /// <returns></returns>
        public static string GetEnvironmentName(string path)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            catch (Exception e)
            {
                Debug.LogFormat("Error trying to get filename of environment: {0} ({1})", path, e.Message);
            }

            // Fall back to default
            return Config.Environment_None;
        }

        /// <summary>
        /// Sets the environment.
        /// </summary>
        /// <param name="relativePath">Path to environment</param>
        /// <returns></returns>
        public IEnumerator SetEnvironment(string relativePath)
        {
            ClearEnvironment();

            if (!IsNoneEnvironment(relativePath))
            {
                var path = MakeAbsolutePath(relativePath);
                Debug.LogFormat("Setting environment to '{0}'", path);

                var bundleLoadRequest = AssetBundle.LoadFromFileAsync(path);
                yield return bundleLoadRequest;

                var assetBundle = bundleLoadRequest.assetBundle;

                if (assetBundle == null)
                {
                    Debug.LogError($"assetBundle is null, path: {path}");
                    yield break;
                }

                if (assetBundle.GetAllAssetNames().Length == 0)
                {
                    Debug.Log($"No asset names found for {path}");
                    yield break;
                }

                try
                {
                    var assetName = assetBundle.GetAllAssetNames()[0];
                    var assetLoadRequest = assetBundle.LoadAssetAsync<GameObject>(assetName);
                    yield return assetLoadRequest;

                    Debug.Log($"Loaded asset name {assetName}");

                    try
                    {
                        GameObject prefab = assetLoadRequest.asset as GameObject;
                        Instantiate(prefab, this.envParentContainer.transform);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw;
                    }
                }
                finally
                {
                    if (null != assetBundle)
                    {
                        assetBundle.Unload(false);
                    }
                }
            }
        }

        /// <summary>
        /// Clears existing environment
        /// </summary>
        /// <returns></returns>
        public void ClearEnvironment()
        {
            // Clear existing environment
            foreach (Transform child in this.envParentContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Populates the list of environments available for pick from
        /// </summary>
        /// <returns></returns>
        private async Task PopulateAsync()
        {
            // Get list of environments in background
            var environments = await Task.Run(() =>
            {
                return EnumerateEnvironmentFiles();
            });

            // Clear existing list
            foreach (Transform child in this.contentTransform)
            {
                GameObject.Destroy(child.gameObject);
            }

            // Create none entry first
            CreateEntry(Config.Environment_None, Config.Environment_None);

            // Populate list of environments
            foreach (var env in environments.OrderBy(key => key.Key))
            {
                CreateEntry(env.Key, env.Value);
            }
        }

        /// <summary>
        /// Instantiate EnvironmentEntry
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        private void CreateEntry(string name, string path)
        {
            var newObj = (GameObject)Instantiate(this.prefabEnvironmentEntry, this.contentTransform);
            var entry = newObj.GetComponent<EnvironmentEntry>();
            entry.text.text = name;
            entry.path = path;
        }

        /// <summary>
        /// Construct map of environment name -> path
        /// </summary>
        /// <returns>Returned map</returns>
        private Dictionary<string, string> EnumerateEnvironmentFiles()
        {
            var environments = new Dictionary<string, string>();

            // Enumerate prefab files
            foreach (var filePath in Directory.GetFiles(
                GetOrCreateEnvironmentPath(), PrefabExtSearch))
            {
                environments[Path.GetFileNameWithoutExtension(filePath)] = MakeRelativePath(filePath);
            }

            return environments;
        }

        private void EnableBorder(Transform t, bool enable)
        {
            var border = t.Find("Border");
            border?.gameObject.SetActive(enable);
        }

        static private string GetOrCreateEnvironmentPath()
        {
            string path = Path.Combine(AppConfig.persistentDataPath, EnvironmentFolder);
            Directory.CreateDirectory(path);
            return path;
        }

        static private string MakeRelativePath(string path)
        {
            return path.Substring(AppConfig.persistentDataPath.Length + 1);
        }

        static private string MakeAbsolutePath(string path)
        {
            return Path.Combine(AppConfig.persistentDataPath, path);
        }

        static public bool IsNoneEnvironment(string path)
        {
            return Config.Environment_None.Equals(path, StringComparison.OrdinalIgnoreCase);
        }

        static public string GetEnvironmentNameFromPath(string path)
        {
            if (IsNoneEnvironment(path))
            {
                return Config.Environment_None;
            }

            try
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            catch (Exception e)
            {
                // Fall back to default
                Debug.LogFormat("Error trying to get filename of path: {0} ({1})", path, e.Message);
            }

            return Config.Environment_None;
        }
    }
}