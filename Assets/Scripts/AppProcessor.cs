using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuestAppLauncher
{
    /// <summary>
    /// Class used to track details about an app
    /// </summary>
    public class ProcessedApp
    {
        public int Index;
        public string PackageName;
        public string AppName;
        public string AutoTabName;
        public string Tab1Name;
        public string Tab2Name;
        public string IconPath;
        public long LastTimeUsed;
    }

    public class AppProcessor
    {
        /// <summary>
        /// Class used to deserialize appnames.json entries
        /// </summary>
        [Serializable]
        class JsonAppNamesEntry
        {
            public string Name;
            public string Category;
            public string Category2;
        }

        // File name of app name overrides
        const string AppNameOverrideTxtFileSearch = "appnames*.txt";
        const string AppNameOverrideJsonFileSearch = "appnames*.json";

        // Icon pack search string
        const string IconPackSearch = "iconpack*.zip";

        // File name of excluded package names
        const string ExcludedPackagesFile = "excludedpackages.txt";

        // Icon pack extraction dir
        const string IconPackExtractionDir = "cache";

        // Extension search for icon overrides
        const string IconOverrideExtSearch = "*.jpg";

        // Built-in tab names
        public const string Tab_Quest = "Quest";
        public const string Tab_Go = "Go/Gear";
        public const string Tab_2D = "2D";
        public const string Tab_All = "All";

        // LastUsage lookback days
        const int LastUsedLookbackDays = 30;

        public static readonly string[] Auto_Tabs = { Tab_Quest, Tab_Go, Tab_2D };

        public static Dictionary<string, ProcessedApp> ProcessApps(Config config)
        {
            var persistentDataPath = UnityEngine.Application.persistentDataPath;
            Debug.Log("Persistent data path: " + persistentDataPath);

            // Dictionary to hold package name -> app index, app name
            var apps = new Dictionary<string, ProcessedApp>(StringComparer.OrdinalIgnoreCase);
            var excludedPackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {

                // Get # of installed apps
                int numApps = currentActivity.Call<int>("getSize");
                Debug.Log("# installed apps: " + numApps);

                var processedLastTimeUsed = false;
                if (config.sortMode.Equals(Config.Sort_MostRecent, StringComparison.OrdinalIgnoreCase))
                {
                    // Process last time used for each app
                    currentActivity.Call("processLastTimeUsed", LastUsedLookbackDays);
                    processedLastTimeUsed = true;
                }

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
                    var lastTimeUsed = processedLastTimeUsed ? currentActivity.Call<long>("getLastTimeUsed", i) : 0;

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

                    apps.Add(packageName, new ProcessedApp { PackageName = packageName, Index = i,
                        AutoTabName = tabName, AppName = appName, LastTimeUsed = lastTimeUsed });
                    Debug.LogFormat("[{0}] package: {1}, name: {2}, auto tab: {3}", i, packageName, appName, tabName);
                }

                // Process app name overrides files (both downloaded & manually created)
                ProcessAppNameOverrideFiles(apps, AssetsDownloader.GetOrCreateDownloadPath());
                ProcessAppNameOverrideFiles(apps, persistentDataPath);

                // Extract icon packs (both downloaded & manually created)
                ExtractIconPacks(currentActivity, AssetsDownloader.GetOrCreateDownloadPath());
                ExtractIconPacks(currentActivity, persistentDataPath);

                // Process extracted icons (both downloaded & manually created)
                ProcessExtractedIcons(apps, AssetsDownloader.GetOrCreateDownloadPath());
                ProcessExtractedIcons(apps, persistentDataPath);

                // Process any individual icons
                var iconOverridePath = persistentDataPath;
                if (Directory.Exists(persistentDataPath))
                {
                    ProcessIconsInPath(apps, persistentDataPath);
                }
            }

            return apps;
        }

        private static void ProcessAppNameOverrideFiles(Dictionary<string, ProcessedApp> apps, string path)
        {
            // Process appname*.json files, sorted by name
            foreach (var filePath in Directory.GetFiles(
                path, AppNameOverrideJsonFileSearch).OrderBy(f => f))
            {
                ProcessAppNameOverrideJsonFile(apps, filePath);
            }

            // Process appname*.txt files, sorted by name
            foreach (var filePath in Directory.GetFiles(
                path, AppNameOverrideTxtFileSearch).OrderBy(f => f))
            {
                ProcessAppNameOverrideTxtFile(apps, filePath);
            }
        }

        private static void ProcessAppNameOverrideJsonFile(Dictionary<string, ProcessedApp> apps, string appNameOverrideFilePath)
        {
            // Override app names, if any
            Debug.Log("Found file: " + appNameOverrideFilePath);

            try
            {
                var json = File.ReadAllText(appNameOverrideFilePath, Encoding.UTF8);
                var jsonAppNames = JsonConvert.DeserializeObject<Dictionary<string, JsonAppNamesEntry>>(json);

                foreach (var entry in jsonAppNames)
                {
                    var packageName = entry.Key;
                    var appName = entry.Value.Name;

                    if (!apps.ContainsKey(packageName))
                    {
                        // App is not installed, so skip
                        continue;
                    }

                    // Get the custom tab names, if any
                    string autoTabName = null;
                    var tab1 = entry.Value.Category;
                    var tab2 = entry.Value.Category2;

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

                    // Override auto tab name if custom name matches built-in tab name
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
                        PackageName = apps[entry.Key].PackageName,
                        Index = apps[entry.Key].Index,
                        AppName = appName,
                        AutoTabName = autoTabName ?? apps[entry.Key].AutoTabName,
                        Tab1Name = tab1 ?? apps[entry.Key].Tab1Name,
                        Tab2Name = tab2 ?? apps[entry.Key].Tab2Name,
                        LastTimeUsed = apps[entry.Key].LastTimeUsed
                    };
                }
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Failed to process json app names: {0}", e.Message));
                return;
            }
        }

        private static void ProcessAppNameOverrideTxtFile(Dictionary<string, ProcessedApp> apps, string appNameOverrideFilePath)
        {
            // Override app names, if any
            // This is just a file with comma-separated packageName,appName[,category1[, category2]]
            // Category1 and category2 are optional categories (tabs).
            Debug.Log("Found file: " + appNameOverrideFilePath);

            string[] lines = File.ReadAllLines(appNameOverrideFilePath, Encoding.UTF8);
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

                // Override auto tab name if custom name matches built-in tab name
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
                    LastTimeUsed = apps[entry[0]].LastTimeUsed
                };
            }
        }

        private static void ExtractIconPacks(AndroidJavaObject currentActivity, string iconPacksPath)
        {
            if (!Directory.Exists(iconPacksPath))
            {
                return;
            }

            var iconPackDestinationFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Full path of extraction dir
            var extractionDirPath = Path.Combine(iconPacksPath, IconPackExtractionDir);
            Debug.LogFormat("Extraction dir: {0}", extractionDirPath);

            // Enumerate all iconpack *.zip files, sorted by name
            foreach (var iconPackFilePath in Directory.GetFiles(iconPacksPath, IconPackSearch).OrderBy(f => f))
            {
                var iconPackFileName = Path.GetFileName(iconPackFilePath);

                // Get file modified date
                var modifiedTime = File.GetLastWriteTime(iconPackFilePath);

                // Construct destination folder name w/ modified time
                var destinationFolderName = iconPackFileName + "_" + modifiedTime.ToString("yyyy-dd-MM--HH-mm-ss");

                iconPackDestinationFolders.Add(destinationFolderName, iconPackFileName);
            }

            // Enumerate all folders under destination path
            if (Directory.Exists(extractionDirPath))
            {
                var dirs = Directory.GetDirectories(extractionDirPath);
                foreach (var dirPath in dirs)
                {
                    var dir = new DirectoryInfo(dirPath).Name;
                    if (iconPackDestinationFolders.ContainsKey(dir))
                    {
                        // Remove matching entry - this means that we've already extracted and matched on modified time
                        iconPackDestinationFolders.Remove(dir);
                    }
                    else
                    {
                        // Delete any folder that is not in the icon pack target destination path
                        Directory.Delete(dirPath, true);
                    }
                }
            }

            // Unzip icon packs
            foreach (var iconPack in iconPackDestinationFolders)
            {
                currentActivity.CallStatic("unzip", Path.Combine(iconPacksPath, iconPack.Value),
                    Path.Combine(extractionDirPath, iconPack.Key));
            }
        }

        private static void ProcessExtractedIcons(Dictionary<string, ProcessedApp> apps, string iconsPath)
        {
            // Full path of extraction dir
            var extractionDirPath = Path.Combine(iconsPath, IconPackExtractionDir);

            if (Directory.Exists(extractionDirPath))
            {
                // Enumerate extracted icon packs, sorted alphabetically
                var dirs = Directory.GetDirectories(extractionDirPath).OrderBy(f => f);
                foreach (var dir in dirs)
                {
                    ProcessIconsInPath(apps, dir);
                }
            }
        }

        private static void ProcessIconsInPath(Dictionary<string, ProcessedApp> apps, string path)
        {
            foreach (var iconFilePath in Directory.GetFiles(path, IconOverrideExtSearch))
            {
                // This is a list of jpg images stored as packageName.jpg.
                var entry = Path.GetFileNameWithoutExtension(iconFilePath);
                if (apps.ContainsKey(entry))
                {
                    Debug.Log("Found icon override: " + iconFilePath);

                    ProcessedApp newProcessedApp = apps[entry];
                    newProcessedApp.IconPath = iconFilePath;
                    apps[entry] = newProcessedApp;
                }
            }
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