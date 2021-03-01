using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuestAppLauncher
{
    /// <summary>
    /// Root config object
    /// </summary>
    [Serializable]
    public class Config
    {
        // Supported category mode
        public const string Category_Off = "off";
        public const string Category_Top = "top";
        public const string Category_Left = "left";
        public const string Category_Right = "right";

        // Sort settings
        public const string Sort_AZ = "az";
        public const string Sort_MostRecent = "mostRecent";

        // Download repos
        public const string DownloadRepo_Type_GitHub = "github";
        public const string DownloadRepo_Default = @"tverona1/QuestAppLauncher_Assets/releases/latest";

        // Background
        public const string Background_Default = "default";

        // Environment
        public const string Environment_None = "none";

        /// <summary>
        /// Grid size
        /// </summary>
        [Serializable]
        public class GridSize
        {
            public int rows = 3;
            public int cols = 3;
        }

        /// <summary>
        /// Download repo type
        /// </summary>
        [Serializable]
        public class DownloadRepo
        {
            public string repoUri;
            public string type;
        }

        // Grid size, specified as cols x rows
        public GridSize gridSize = new GridSize();

        // Sort mode
        public string sortMode = Sort_AZ;

        // Whether to show 2D apps
        public bool show2D = true;

        // Auto Category: Apps are automatically categorized into 3 tabs - Quest, Go/GearVr, 2D
        public string autoCategory = Category_Top;

        // Custom Category: Apps are categorized according to appnames.txt file
        public string customCategory = Category_Right;

        // Whether to auto-download updates
        public bool autoUpdate = true;

        // Background image path
        public string background = Background_Default;

        // Environment path
        public string environment = Environment_None;

        // Github download repos
        public List<DownloadRepo> downloadRepos = new List<DownloadRepo>();

        public Config(bool initDefaults = false)
        {
            if (initDefaults)
            {
                // We must initialize any default collection values here. Otherwise, if we initialize them inline,
                // we'll keep adding duplicate values whenever we persist via JSON.NET (since it invokes the default contructor as part
                // of deserialization, which again adds the default value).
                this.downloadRepos.Add(new DownloadRepo { repoUri = DownloadRepo_Default, type = DownloadRepo_Type_GitHub });
            }
        }
    }

    /// <summary>
    /// Class responsible for loading / saving config into a config.json file.
    /// </summary>
    public class ConfigPersistence
    {
        // File name of app name overrides
        const string ConfigFileName = "config.json";

        /// <summary>
        /// Load config from file
        /// </summary>
        /// <param name="config"></param>
        /// <returns>Config object</returns>
        static public Config LoadConfig()
        {
            var configFilePath = Path.Combine(AppConfig.persistentDataPath, ConfigFileName);
            if (File.Exists(configFilePath))
            {
                Debug.Log("Found config file: " + configFilePath);
                var jsonConfig = File.ReadAllText(configFilePath);

                try
                {
                    return JsonConvert.DeserializeObject<Config>(jsonConfig);
                }
                catch (Exception e)
                {
                    Debug.Log(string.Format("Failed to read config: {0}", e.Message));
                }
            }
            else
            {
                Debug.Log("Did not find config file: " + configFilePath);
            }

            // Return default config
            return new Config(true);
        }

        /// <summary>
        /// Save config to a file
        /// </summary>
        /// <param name="config"></param>
        static public void SaveConfig(Config config)
        {
            var configFilePath = Path.Combine(AppConfig.persistentDataPath, ConfigFileName);
            Debug.Log("Saving config file: " + configFilePath);

            try
            {
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Failed to write config: {0}", e.Message));
            }
        }
    }
}