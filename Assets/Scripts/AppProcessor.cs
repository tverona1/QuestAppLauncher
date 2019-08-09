using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QuestAppLauncher
{
    public class ProcessedApp
    {
        public int Index;
        public string PackageName;
        public string AppName;
        public string AutoTabName;
        public string Tab1Name;
        public string Tab2Name;
        public string IconPath;
    }

    public class AppProcessor
    {
        // File name of app name overrides
        const string AppNameOverrideFile = "appnames.txt";

        // File name of excluded package names
        const string ExcludedPackagesFile = "excludedpackages.txt";

        // Extension search for icon overrides
        const string IconOverrideExtSearch = "*.jpg";

        // Built-in tab names
        public const string Tab_Quest = "Quest";
        public const string Tab_Go = "Go/Gear";
        public const string Tab_2D = "2D";
        public const string Tab_All = "All";

        public static readonly string[] Auto_Tabs = { Tab_Quest, Tab_Go, Tab_2D };

        public static Dictionary<string, ProcessedApp> ProcessApps(Config config)
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            Debug.Log("Persistent data path: " + persistentDataPath);

            // Dictionary to hold package name -> app index, app name
            var apps = new Dictionary<string, ProcessedApp>();
            var excludedPackageNames = new HashSet<string>();

            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {

                // Get # of installed apps
                int numApps = currentActivity.Call<int>("getSize");
                Debug.Log("# installed apps: " + numApps);

                // Add current package name to excludedPackageNames to filter it out
                excludedPackageNames.Add(currentActivity.Call<string>("getPackageName"));

                // This is a file containing packageNames that will be excluded
                var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);
                if (File.Exists(excludedPackageNamesFilePath))
                {
                    Debug.Log("Found file: " + excludedPackageNamesFilePath);
                    string[] excludedPackages = File.ReadAllLines(excludedPackageNamesFilePath);
                    foreach (string excludedPackage in excludedPackages)
                    {
                        if (!string.IsNullOrEmpty(excludedPackage))
                        {
                            excludedPackageNames.Add(excludedPackage);
                        }
                    }
                }

                // Get installed package and app names
                for (int i = 0; i < numApps; i++)
                {
                    var packageName = currentActivity.Call<string>("getPackageName", i);
                    var appName = currentActivity.Call<string>("getAppName", i);

                    if (excludedPackageNames.Contains(packageName))
                    {
                        // Skip excluded package
                        Debug.LogFormat("Skipping Excluded [{0}] Package: {1}, name: {2}", i, packageName, appName);
                        continue;
                    }

                    // Determine app type (Quest, Go or 2D)
                    string tabName;
                    if (currentActivity.Call<bool>("is2DApp", i))
                    {
                        if (!config.show2D)
                        {
                            // Skip 2D apps
                            Debug.LogFormat("Skipping 2D [{0}] Package: {1}, name: {2}", i, packageName, appName);
                            continue;
                        }

                        tabName = Tab_2D;
                    }
                    else if (currentActivity.Call<bool>("isQuestApp", i))
                    {
                        tabName = Tab_Quest;
                    }
                    else
                    {
                        tabName = Tab_Go;
                    }

                    apps.Add(packageName, new ProcessedApp { PackageName = packageName, Index = i, AutoTabName = tabName, AppName = appName });
                    Debug.LogFormat("[{0}] package: {1}, name: {2}, auto tab: {3}", i, packageName, appName, tabName);
                }

                // Override app names, if any
                // This is just a file with comma-separated packageName,appName[,category1[, category2]]
                // Category1 and category2 are optional categories (tabs).
                var appNameOverrideFilePath = Path.Combine(persistentDataPath, AppNameOverrideFile);
                if (File.Exists(appNameOverrideFilePath))
                {
                    Debug.Log("Found file: " + appNameOverrideFilePath);
                    string[] lines = File.ReadAllLines(appNameOverrideFilePath);
                    foreach (string line in lines)
                    {
                        line.Trim();

                        if (line.StartsWith("#"))
                        {
                            // Skip comments
                            continue;
                        }

                        // Parse line
                        var entry = line.Split(',');
                        if (entry.Length < 2)
                        {
                            // We expect at least two entries
                            continue;
                        }

                        var packageName = entry[0];
                        var appName = entry[1];

                        if (!apps.ContainsKey(packageName))
                        {
                            // App is not installed, so skip
                            continue;
                        }

                        // Get the custom tab names, if any
                        string autoTabName = null;
                        var tab1 = entry.Length > 2 ? entry[2] : null;
                        var tab2 = entry.Length > 3 ? entry[3] : null;

                        if (tab1 != null && tab1.Length == 0)
                        {
                            tab1 = null;
                        }

                        if (tab2 != null && tab2.Length == 0)
                        {
                            tab2 = null;
                        }

                        if (tab1 != null && tab2 != null && tab1.Equals(tab2, StringComparison.OrdinalIgnoreCase))
                        {
                            tab2 = null;
                        }

                        // Override auto tabe name if custom name matches built-in tab name
                        if (tab1 != null && Auto_Tabs.Contains(tab1, StringComparer.OrdinalIgnoreCase))
                        {
                            autoTabName = tab1;
                            tab1 = null;
                        }

                        if (tab2 != null && Auto_Tabs.Contains(tab2, StringComparer.OrdinalIgnoreCase))
                        {
                            autoTabName = tab2;
                            tab2 = null;
                        }

                        // Update entry
                        apps[packageName] = new ProcessedApp
                        {
                            PackageName = apps[entry[0]].PackageName,
                            Index = apps[entry[0]].Index,
                            AppName = appName,
                            AutoTabName = autoTabName ?? apps[entry[0]].AutoTabName,
                            Tab1Name = tab1 ?? apps[entry[0]].Tab1Name,
                            Tab2Name = tab2 ?? apps[entry[0]].Tab2Name,
                        };
                    }
                }
                else
                {
                    Debug.Log("Did not find: " + appNameOverrideFilePath);
                }

                // Load list of app icon overrides
                // This is a list of jpg images stored as packageName.jpg.
                var iconOverridePath = persistentDataPath;
                if (Directory.Exists(iconOverridePath))
                {
                    foreach (var iconFileName in Directory.GetFiles(iconOverridePath, IconOverrideExtSearch))
                    {
                        var entry = Path.GetFileNameWithoutExtension(iconFileName);
                        if (apps.ContainsKey(entry))
                        {
                            Debug.Log("Found icon override: " + iconFileName);

                            ProcessedApp newProcessedApp = apps[entry];
                            newProcessedApp.IconPath = Path.Combine(iconOverridePath, iconFileName);
                            apps[entry] = newProcessedApp;
                        }
                    }
                }
            }

            return apps;
        }

        public static byte[] GetAppIcon(string iconPath, int appIndex)
        {
            byte[] bytesIcon = null;
            bool useApkIcon = true;
            if (null != iconPath)
            {
                // Use overridden icon
                try
                {
                    bytesIcon = File.ReadAllBytes(iconPath);
                    useApkIcon = false;
                }
                catch (Exception e)
                {
                    // Fall back to using the apk icon
                    Debug.Log(string.Format("Error reading app icon from file [{0}]: {1}", iconPath, e.Message));
                }
            }

            if (useApkIcon)
            {
                // Use built-in icon from the apk
                using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    bytesIcon = (byte[])(Array)currentActivity.Call<sbyte[]>("getIcon", appIndex);
                }
            }

            return bytesIcon;
        }

        /// <summary>
        /// Static method to add a package name to the excludedFile
        /// </summary>
        /// <param name="packageName"></param>
        static public void AddAppToExcludedFile(string packageName)
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);

            using (StreamWriter writer = File.AppendText(excludedPackageNamesFilePath))
            {
                writer.WriteLine(packageName);
                Debug.Log($"Added package {packageName} to the excluded file {excludedPackageNamesFilePath}");
            }
        }

        /// <summary>
        /// Static method to delete the excludedFile
        /// </summary>
        /// <returns>true if file exists</returns>
        static public bool DeleteExcludedAppsFile()
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            var excludedPackageNamesFilePath = Path.Combine(persistentDataPath, ExcludedPackagesFile);

            if (File.Exists(excludedPackageNamesFilePath))
            {
                File.Delete(excludedPackageNamesFilePath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Static method for launching an Android app
        /// </summary>
        /// <param name="packageId"></param>
        static public void LaunchApp(string packageId)
        {
            using (AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager"))
            {
                AndroidJavaObject launchIntent = null;

                try
                {
                    launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", packageId);
                    ca.Call("startActivity", launchIntent);

                    // Quest doesn't like multiple VR apps running simultaneously. Kill ourselves.
                    UnityEngine.Application.Quit();
                }
                catch (System.Exception e)
                {
                    Debug.Log(string.Format("Failed to launch app {0}: {1}", packageId, e.Message));
                }
                finally
                {
                    if (null != launchIntent)
                    {
                        launchIntent.Dispose();
                    }
                }
            }
        }
    }
}