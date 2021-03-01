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
    /// App config object
    /// </summary>
    public class AppConfig
    {
        private static bool isInitialized = false;
        public static string persistentDataPath;

        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            persistentDataPath = UnityEngine.Application.persistentDataPath;

            isInitialized = true;
        }
    }
}