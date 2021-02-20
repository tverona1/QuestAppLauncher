using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuestAppLauncher
{
    /// <summary>
    /// Downloads assets (app icons packs and names files) from configured repos.
    /// </summary>
    public class AssetsDownloader
    {
        // Download cache folder that contains downloaded files
        const string DownloadCacheFolder = "download_cache";

        // Manifest file to track what we've downloaded
        const string DownloadManifestFile = "download_manifest.json";

        // Temporary filename for download
        const string TempDownloadFileExtention = ".tmp_download";

        // GitHub API url
        const string GithubApiUrl = @"http://api.github.com/repos/";

        // Rate limit in minutes
        const int RateLimitInMins = 5;

        // Used for mutual exclusion when loading assets
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Class that tracks each file downloaded.
        /// Used to determine whether an asset has been updated since last download.
        /// </summary>
        [Serializable]
        public class AssetInfo
        {
            // Repo uri
            public string RepoUri;

            // Asset's url
            public string Url;

            // Asset's updated at timestamp
            public string UpdatedAt;

            // Asset's tag name
            public string TagName;
        }

        /// <summary>
        /// Mapping of asset name to asset info above. This is persisted in the download manifest.
        /// </summary>
        [Serializable]
        public class AssetsManifest
        {
            // File name -> release metadata
            public Dictionary<string, AssetInfo> Metadata = new Dictionary<string, AssetInfo>();

            // Last updated timestamp
            public DateTime LastUpdated;
        }

        /// <summary>
        /// Helper method to download assets asynchronously. After completion, scene is automatically reloaded
        /// if new assets have been downloaded.
        /// </summary>
        /// <param name="config">Current config</param>
        /// <param name="downloadProgress">Download progress interface - used to indicate download progress</param>
        /// <returns></returns>
        public static async Task DownloadAssetsAsync(Config config, IDownloadProgress downloadProgress = null, bool forceCheck = false)
        {
            // Start background thread
            Task.Run(async () =>
            {
                // Mutual exclusion while loading assets
                await AssetsDownloader.semaphoreSlim.WaitAsync();

                // Attach / detatch JNI. Required for any calls into JNI from background threads.
                AndroidJNI.AttachCurrentThread();

                try
                {
                    // Download assets from repos.
                    AssetsDownloader assetsDownloader = new AssetsDownloader();
                    return await assetsDownloader.DownloadFromReposAsync(config, downloadProgress, forceCheck);
                }
                finally
                {
                    AssetsDownloader.semaphoreSlim.Release();
                    AndroidJNI.DetachCurrentThread();
                }
            }).ContinueWith((downloadedAssets) =>
            {
                if (downloadedAssets.Result)
                {
                    // We downloaded new assets, so re-load the scene
                    Debug.Log("Downloaded new assets. Re-populating panel");
                    SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Asynchronously download assets from configured repos.
        /// </summary>
        /// <param name="config">Current config</param>
        /// <param name="downloadProgress">Download progress interface</param>
        /// <returns></returns>
        private async Task<bool> DownloadFromReposAsync(Config config, IDownloadProgress downloadProgress = null, bool forceCheck = false)
        {
            if (null == config.downloadRepos)
            {
                // No repos configured, so return
                return false;
            }

            // Load the download manifest. This is used to compare if we're up-to-date or not.
            AssetsManifest manifest = LoadManifest();
            if (null == manifest)
            {
                manifest = new AssetsManifest();
            }

            // Rate limit update checks to one per couple of minutes, to avoid GitHub's rate limit
            if (!forceCheck && DateTime.Now.Subtract(manifest.LastUpdated).TotalMinutes < RateLimitInMins)
            {
                Debug.LogFormat("Exceeded rate limit of {0} mins - last checked for update on {1}", RateLimitInMins, manifest.LastUpdated);
                return false;
            }

            // Mark that we've just checked for updates & update manifest
            manifest.LastUpdated = DateTime.Now;
            SaveManifest(manifest);

            if (null != downloadProgress)
            {
                downloadProgress.OnCheckingForUpdates();
            }

            // Download the assets metadata - used to determine whether we are up-to-date or not
            var assetsInfo = await DownloadAssetsMetadata(config, manifest, downloadProgress, forceCheck);
            if (assetsInfo.Count == 0)
            {
                // No updates have been found, so return
                Debug.Log("No updates found");
                if (null != downloadProgress)
                {
                    downloadProgress.OnNoUpdatesAvailable();
                }

                return false;
            }

            // Download the assets
            var downloadedAssets = await DownloadFromReposInternalAsync(manifest, assetsInfo, downloadProgress);
            if (null != downloadProgress)
            {
                downloadProgress.OnUpdateFinish();
            }

            return downloadedAssets;
        }

        /// <summary>
        /// Downloads assets metadata. This is used to determine whether our assets are up-to-date.
        /// </summary>
        /// <param name="config">Current config</param>
        /// <param name="downloadProgress">Download progress interface</param>
        /// <returns></returns>
        private async Task<Dictionary<string, AssetInfo>> DownloadAssetsMetadata(
            Config config, AssetsManifest manifest, IDownloadProgress downloadProgress = null, bool forceCheck = false)
        {
            // Get asset info from repos
            var assetsInfo = new Dictionary<string, AssetInfo>(StringComparer.OrdinalIgnoreCase);

            // Get the set of repo URIs (removing any duplicates)
            var configRepos = new HashSet<string>();
            foreach (var item in config.downloadRepos)
            {
                if (null == item.type || !string.Equals(item.type, Config.DownloadRepo_Type_GitHub, StringComparison.OrdinalIgnoreCase))
                {
                    // For now, we only support GitHub repos
                    continue;
                }

                configRepos.Add(item.repoUri);
            }

            var reposLoaded = new HashSet<string>();
            foreach (var repoUri in configRepos)
            {
                // Get assets from the GitHub repo
                var repoLoaded = await GetAssetsInfoFromGithubRepoAsync(repoUri, assetsInfo, downloadProgress);
                if (repoLoaded)
                {
                    reposLoaded.Add(repoUri);
                }
            }

            Debug.LogFormat("Assets info contains {0} entries", assetsInfo.Count);

            // Enumerate asset metadata in our download manifest
            var deletedAssets = new HashSet<string>();
            foreach (var entry in manifest.Metadata)
            {
                if (assetsInfo.ContainsKey(entry.Key))
                {
                    if (string.Equals(entry.Value.UpdatedAt, assetsInfo[entry.Key].UpdatedAt, StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(Path.Combine(GetOrCreateDownloadPath(), entry.Key)))
                    {
                        // Matched on file name and updated-at timestamp, so we're up to date. Skip this one.
                        Debug.LogFormat("Asset {0} is already up to date ({1})", entry.Key, entry.Value.UpdatedAt);
                        assetsInfo.Remove(entry.Key);
                    }
                }
                else if (reposLoaded.Contains(entry.Value.RepoUri))
                {
                    // The repo no longer has the cached file, so nuke it to keep things in sync
                    var filePath = Path.Combine(GetOrCreateDownloadPath(), entry.Key);
                    if (File.Exists(filePath))
                    {
                        Debug.LogFormat("Deleting cached file that no longer exists on server: {0} @ {1}", filePath, entry.Value.RepoUri);
                        File.Delete(filePath);
                    }
                    deletedAssets.Add(entry.Key);
                }
            }

            // Remove old cached files
            foreach (var entry in deletedAssets)
            {
                manifest.Metadata.Remove(entry);
            }

            if (deletedAssets.Count > 0)
            {
                // Persist manifest
                SaveManifest(manifest);
            }

            return assetsInfo;
        }

        /// <summary>
        /// Download assets from repos. The download manifest is also updated.
        /// </summary>
        /// <param name="manifest">Download manifest</param>
        /// <param name="assetsInfo">Assets to download</param>
        /// <param name="downloadProgress">Download progress interface</param>
        /// <returns></returns>
        private async Task<bool> DownloadFromReposInternalAsync(
            AssetsManifest manifest, Dictionary<string, AssetInfo> assetsInfo, IDownloadProgress downloadProgress = null)
        {
            var downloadedAsset = false;

            foreach (var entry in assetsInfo)
            {
                // Download asset
                var success = await DownloadAssetFromGitHubRepoAsync(entry.Key, entry.Value, downloadProgress);
                if (success)
                {
                    // Update manifest
                    manifest.Metadata[entry.Key] = entry.Value;
                    downloadedAsset = true;
                }
            }

            if (downloadedAsset)
            {
                // Persist manifest
                SaveManifest(manifest);
            }

            return downloadedAsset;
        }

        /// <summary>
        /// Download asset metadata from given uri. The assets info parameter is populate with the metadata.
        /// </summary>
        /// <param name="repoUri">The repo uri to download</param>
        /// <param name="assetsInfo">Assets info mapping that is populate by this function</param>
        /// <param name="downloadProgress">Download progress interface</param>
        /// <returns></returns>
        private async Task<bool> GetAssetsInfoFromGithubRepoAsync(string repoUri,
            Dictionary<string, AssetInfo> assetsInfo, IDownloadProgress downloadProgress = null)
        {
            var requestUrl = GithubApiUrl + repoUri;
            Debug.LogFormat("Reading assets from {0}", requestUrl);

            try
            {
                // Request asset url

                // System.Net.WebClient();
                using (var req = new WebRequest(requestUrl))
                {
                    req.downloadHandler = new DownloadHandlerText();
                    await req.SendWebRequest();
                    if (req.isNetworkError || req.isHttpError)
                    {
                        // Error reading asset metadata, so return.
                        Debug.LogFormat($"Error reading asset info: {req.error}. isNetworkError: {req.isNetworkError}. isHttpError: {req.isHttpError}");

                        var responseHeaders = req.GetResponseHeaders();
                        if (null != responseHeaders && responseHeaders.Contains("X-RateLimit-Remaining") &&
                            responseHeaders.GetValues("X-RateLimit-Remaining").First() == "0")
                        {
                            // Github request limit reached. Display a friendly error message.
                            Debug.LogFormat("Request limit reached");

                            if (null != downloadProgress)
                            {
                                downloadProgress.OnError(string.Format("Error updating: Request Limit Reached - try again later. ({1})",
                                    req.error, requestUrl));
                            }
                        }
                        else
                        {
                            if (null != downloadProgress)
                            {
                                downloadProgress.OnError(string.Format("Error updating: {0} ({1})",
                                    req.error, requestUrl));
                            }
                        }
                        return false;
                    }

                    // Parse the returned asset metadata
                    var jObject = JObject.Parse((req.downloadHandler as DownloadHandlerText).text);
                    var tagName = jObject["tag_name"].Value<string>();

                    foreach (var property in jObject["assets"])
                    {
                        var updatedAt = property["updated_at"].Value<string>();
                        var url = property["url"].Value<string>();
                        var name = property["name"].Value<string>();

                        // For now, simply accept any iconpack*.zip and appnames*.txt/json.
                        if ((name.StartsWith("iconpack") && name.EndsWith(".zip")) ||
                            (name.StartsWith("appnames") && name.EndsWith(".txt")) ||
                            (name.StartsWith("appnames") && name.EndsWith(".json")))
                        {
                            assetsInfo[name] = new AssetInfo { RepoUri = repoUri, Url = url, UpdatedAt = updatedAt, TagName = tagName };
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Exception reading asset info: {0} ", e.Message);
                if (null != downloadProgress)
                {
                    downloadProgress.OnError(string.Format("Error updating: {0} ({1})",
                        e.Message, requestUrl));
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Download a single asset (file) from repo
        /// </summary>
        /// <param name="name">Name of the asset (file) to download</param>
        /// <param name="assetInfo">Asset info (metadata)</param>
        /// <param name="downloadProgress">Download progress interface</param>
        /// <returns></returns>
        private async Task<bool> DownloadAssetFromGitHubRepoAsync(string name, AssetInfo assetInfo,
            IDownloadProgress downloadProgress)
        {
            var filePath = Path.Combine(GetOrCreateDownloadPath(), name);
            var tempFilePath = filePath + TempDownloadFileExtention;
            Debug.LogFormat("Downloading asset {0} from {1}", filePath, assetInfo.Url);

            try
            {
                // Request asset url
                using (var req = new WebRequest(assetInfo.Url))
                {
                    req.SetRequestHeader("Accept", "application/octet-stream");
                    if (null != downloadProgress)
                    {
                        downloadProgress.OnDownloadStart(name);
                    }
                    var downloadHandler = new DownloadHandlerFileWithProgress(tempFilePath, downloadProgress.OnDownloadProgress);
                    downloadHandler.removeFileOnAbort = true;
                    req.downloadHandler = downloadHandler;
                    await req.SendWebRequest();

                    if (req.isNetworkError || req.isHttpError)
                    {
                        // Error reading asset metadata, so return.
                        Debug.LogFormat("Error downloading asset: {0}", req.error);
                        if (null != downloadProgress)
                        {
                            downloadProgress.OnError(string.Format("Error updating: {0} ({1})",
                                req.error, assetInfo.Url));
                        }

                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }

                        return false;
                    }

                    // Rename temp file to desination file to ensure that we are not downloading
                    // an error body message into the destination file.
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    File.Move(tempFilePath, filePath);

                    if (null != downloadProgress)
                    {
                        downloadProgress.OnDownloadFinish();
                    }

                    Debug.LogFormat($"Successfully downloaded {name}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogFormat("Exception downloading asset: {0} ", e.Message);
                if (null != downloadProgress)
                {
                    downloadProgress.OnError(string.Format("Error updating: {0} ({1})",
                        e.Message, assetInfo.Url));
                }
            }

            return false;
        }

        /// <summary>
        /// Load download manifest
        /// </summary>
        /// <returns>Download manifest</returns>
        static private AssetsManifest LoadManifest()
        {
            var manifestPath = Path.Combine(GetOrCreateDownloadPath(), DownloadManifestFile);
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            Debug.Log("Found manifest: " + manifestPath);
            var jsonManifest = File.ReadAllText(manifestPath);

            try
            {
                AssetsManifest manifest = new AssetsManifest();
                return JsonConvert.DeserializeObject<AssetsManifest>(jsonManifest);
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Failed to read manifest: {0}", e.Message));
            }

            return null;
        }

        /// <summary>
        /// Persist download manifest
        /// </summary>
        /// <param name="manifest">Download manifest to persist</param>
        static private void SaveManifest(AssetsManifest manifest)
        {
            var manifestPath = Path.Combine(GetOrCreateDownloadPath(), DownloadManifestFile);
            Debug.Log("Saving manifest: " + manifestPath);

            try
            {
                File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Failed to write manifest: {0}", e.Message));
            }
        }

        static public string GetOrCreateDownloadPath()
        {
            string path = Path.Combine(AppConfig.persistentDataPath, DownloadCacheFolder);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
