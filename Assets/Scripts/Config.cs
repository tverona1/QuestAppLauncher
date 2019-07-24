using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuestAppLauncher
{
    /// <summary>
    /// Root config object
    /// </summary>
    [Serializable]
    public class Config
    {
        /// <summary>
        /// Grid size
        /// </summary>
        [Serializable]
        public class GridSize
        {
            public int rows = 3;
            public int cols = 3;
        }

        public GridSize gridSize;
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
        /// <param name="config">Config object that will be overwritten</param>
        static public void LoadConfig(Config config)
        {
            var configFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, ConfigFileName);
            if (File.Exists(configFilePath))
            {
                Debug.Log("Found config file: " + configFilePath);
                var jsonConfig = File.ReadAllText(configFilePath);

                try
                {
                    JsonUtility.FromJsonOverwrite(jsonConfig, config);
                }
                catch (Exception e)
                {
                    Debug.Log(string.Format("Failed to read config: {0}", e.Message));
                }
            }
        }

        /// <summary>
        /// Save config to a file
        /// </summary>
        /// <param name="config"></param>
        static public void SaveConfig(Config config)
        {
            var configFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, ConfigFileName);
            Debug.Log("Saving config file: " + configFilePath);

            try
            {
                File.WriteAllText(configFilePath, JsonUtility.ToJson(config, true));
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Failed to read config: {0}", e.Message));
            }

        }
    }
}