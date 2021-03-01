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
    public class SkyboxHandler : MonoBehaviour
    {
        // Max pixles of skybox image for both equirectangular and cubemap images.
        // Anything larger we'll scale down. We restrict it primarily due to memory constraints.
        const int MaxPixels = 4096 * 4096;

        // Skybox selected callback
        public Action<string> OnSkyboxSelected;

        // Skyview List Container
        public GameObject skyviewListContainer;

        // Skybox list entry prefab
        public GameObject prefabSkyboxEntry;

        // Content transform
        public Transform contentTransform;

        // Default skybox
        private Material defaultSkybox;

        // Skybox folder
        private const string SkyboxFolder = "backgrounds";

        // Extension search for images
        const string JpgExtSearch = "*.jpg";
        const string PngExtSearch = "*.png";

        /// <summary>
        /// Show the skybox list dialog
        /// </summary>
        public async void ShowList()
        {
            // Show the dialog
            this.skyviewListContainer.SetActive(true);

            // Populate the list
            await PopulateAsync();
        }

        public void OnCancel()
        {
            // Hide the dialog
            this.skyviewListContainer.SetActive(false);
        }

        public void OnHoverEnter(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("SkyboxEntry") as SkyboxEntry;
            if (null != appEntry)
            {
                // Enable border
                EnableBorder(t, true);
            }
        }

        public void OnHoverExit(Transform t)
        {
            var appEntry = t.gameObject.GetComponent("SkyboxEntry") as SkyboxEntry;
            if (null != appEntry)
            {
                // Disable border
                EnableBorder(t, false);
            }
        }

        public async void OnSelected(Transform t)
        {
            var entry = t.gameObject.GetComponent("SkyboxEntry") as SkyboxEntry;
            if (null != entry)
            {
                // Set the skybox
                SetSkybox(entry.path);
                this.skyviewListContainer.SetActive(false);

                // Callback if registered
                if (null != OnSkyboxSelected)
                {
                    OnSkyboxSelected(entry.path);
                }
            }
        }

        /// <summary>
        /// Return skybox name given its path (i.e. filename w/o extension)
        /// </summary>
        /// <param name="skyboxPath">Path to skybox image</param>
        /// <returns></returns>
        public static string GetSkyboxName(string skyboxPath)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(skyboxPath);
            }
            catch (Exception e)
            {
                Debug.LogFormat("Error trying to get filename of skybox: {0} ({1})", skyboxPath, e.Message);
            }

            // Fall back to default
            return Config.Background_Default;
        }

        /// <summary>
        /// Sets the skybox. Supports either equirectangular or cubemap, auto chosen based on aspect ratio.
        /// </summary>
        /// <param name="skyboxPath">Path to skybox image</param>
        /// <returns></returns>
        public async Task SetSkybox(string skyboxPath)
        {
            Debug.LogFormat("Setting skybox to '{0}'", skyboxPath);

            if (null == this.defaultSkybox)
            {
                // Save off the default skybox
                this.defaultSkybox = RenderSettings.skybox;
            }

            if (IsDefaultSkybox(skyboxPath))
            {
                if (RenderSettings.skybox == this.defaultSkybox)
                {
                    // Skip if skybox is already the default
                    Debug.LogFormat("Skybox already default, skipping.");
                    return;
                }

                // Set default skybox
                SetDefaultSkybox();
                return;
            }

            // Destroy existing skybox
            if (null != RenderSettings.skybox && RenderSettings.skybox != this.defaultSkybox)
            {
                // Destroy texture
                var oldTexture = RenderSettings.skybox.GetTexture("_Tex");
                if (null != oldTexture)
                {
                    DestroyImmediate(oldTexture);
                }

                // Destroy material
                DestroyImmediate(RenderSettings.skybox);
                RenderSettings.skybox = null;
            }

            // Read the image
            var result = await AppProcessor.LoadRawImageAsync(MakeAbsoluteSkymapPath(skyboxPath), MaxPixels, true);
            var image = result.Item1;
            var imageWidth = result.Item2;
            var imageHeight = result.Item3;

            if (null == image)
            {
                // Fall back to default skybox
                SetDefaultSkybox();
                return;
            }

            Texture2D texture = null;
            Material material = null;
            bool destroyTexture = false;

            try
            {
                // Load the image into a 2D texture. We decode in background thread (above) in Java and load the raw image here
                // because Texture2D.LoadImage on the main thread can cause significant freezes since it is not async.
                texture = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 16;
                texture.LoadRawTextureData(image);
                texture.Apply();

                if (4 * texture.height == 3 * texture.width)
                {
                    // Texture is a horizontal cross cube map (4:3 aspect ratio).
                    // Load cubemap shader. Also rotate x-axis by 180 degrees to compensate for platform-specific rendering differences
                    // (see https://docs.unity3d.com/Manual/SL-PlatformDifferences.html).
                    Debug.LogFormat("Setting horizontal-cross cubemap skybox");
                    destroyTexture = true;
                    material = new Material(Shader.Find("skybox/cube"));
                    material.SetTexture("_Tex", CubemapFromHorizCrossTexture2D(texture));
                }
                else if (6 * texture.height == texture.width)
                {
                    // Texture is a horizontal cube map (6:1 aspect ratio).
                    // Load cubemap shader. Also rotate x-axis by 180 degrees to compensate for platform-specific rendering differences
                    // (see https://docs.unity3d.com/Manual/SL-PlatformDifferences.html).
                    Debug.LogFormat("Setting horizontal cubemap skybox");
                    destroyTexture = true;
                    material = new Material(Shader.Find("skybox/cube"));
                    material.SetTexture("_Tex", CubemapFromHorizTexture2D(texture));
                }
                else
                {
                    // Texture is equirectangular
                    Debug.LogFormat("Setting equirectangular skybox");
                    material = new Material(Shader.Find("skybox/equirectangular"));
                    material.SetTexture("_Tex", texture);
                }

                RenderSettings.skybox = material;
                DynamicGI.UpdateEnvironment();
            }
            catch (Exception e)
            {
                // Fall back to default skybox
                Debug.LogFormat("Exception: {0}", e.Message);
                SetDefaultSkybox();
            }
            finally
            {
                if (destroyTexture && null != texture)
                {
                    DestroyImmediate(texture);
                }
            }
        }

        /// <summary>
        /// Clears the skybox.
        /// </summary>
        /// <returns></returns>
        public void ClearSkybox()
        {
            Debug.Log("Clearing skybox");

            if (null == this.defaultSkybox)
            {
                // Save off the default skybox
                this.defaultSkybox = RenderSettings.skybox;
            }

            // Destroy existing skybox
            if (null != RenderSettings.skybox && RenderSettings.skybox != this.defaultSkybox)
            {
                // Destroy texture
                var oldTexture = RenderSettings.skybox.GetTexture("_Tex");
                if (null != oldTexture)
                {
                    DestroyImmediate(oldTexture);
                }

                // Destroy material
                DestroyImmediate(RenderSettings.skybox);
            }

            RenderSettings.skybox = null;
        }

        /// <summary>
        /// Gets cubemap from a 2D texture that represents 6-sided cube as a horizontal cross:
        /// [    ]  [ +y ]  [    ]
        /// [ -x ]  [ +z ]  [ +x ]
        /// [    ]  [ -y ]  [    ]
        /// Note: This is less memory efficient than horizontal representation below.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        private static Cubemap CubemapFromHorizCrossTexture2D(Texture2D texture)
        {
            int cubedim = texture.width / 4;
            Cubemap cube = new Cubemap(cubedim, TextureFormat.ARGB32, false);
            cube.SetPixels(texture.GetPixels(0, cubedim, cubedim, cubedim), CubemapFace.NegativeX);
            cube.SetPixels(texture.GetPixels(cubedim, cubedim, cubedim, cubedim), CubemapFace.PositiveZ);
            cube.SetPixels(texture.GetPixels(2 * cubedim, cubedim, cubedim, cubedim), CubemapFace.PositiveX);
            cube.SetPixels(texture.GetPixels(3 * cubedim, cubedim, cubedim, cubedim), CubemapFace.NegativeZ);
            cube.SetPixels(texture.GetPixels(cubedim, 0, cubedim, cubedim), CubemapFace.PositiveY);
            cube.SetPixels(texture.GetPixels(cubedim, 2 * cubedim, cubedim, cubedim), CubemapFace.NegativeY);
            cube.Apply();
            return cube;
        }

        /// <summary>
        /// Gets cubemap from a 2D texture that represents 6-sided cube as a horizontal layout:
        /// [ +x ]  [ -x ]  [ +y ]  [ -y ]  [ +z ]  [ -z ]
        /// Note: This is the recommended representation of cube map as it is more efficient in terms of memory.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        private static Cubemap CubemapFromHorizTexture2D(Texture2D texture)
        {
            int cubedim = texture.height;
            Cubemap cube = new Cubemap(cubedim, TextureFormat.ARGB32, false);
            cube.SetPixels(texture.GetPixels(0, 0, cubedim, cubedim), CubemapFace.PositiveX);
            cube.SetPixels(texture.GetPixels(cubedim, 0, cubedim, cubedim), CubemapFace.NegativeX);
            cube.SetPixels(texture.GetPixels(2 * cubedim, 0, cubedim, cubedim), CubemapFace.PositiveY);
            cube.SetPixels(texture.GetPixels(3 * cubedim, 0, cubedim, cubedim), CubemapFace.NegativeY);
            cube.SetPixels(texture.GetPixels(4 * cubedim, 0, cubedim, cubedim), CubemapFace.PositiveZ);
            cube.SetPixels(texture.GetPixels(5 * cubedim, 0, cubedim, cubedim), CubemapFace.NegativeZ);
            cube.Apply();
            return cube;
        }

        public void SetDefaultSkybox()
        {
            Debug.LogFormat("Setting default skybox");
            RenderSettings.skybox = this.defaultSkybox;
            DynamicGI.UpdateEnvironment();
        }

        /// <summary>
        /// Populates the list of skybox images available for pick from
        /// </summary>
        /// <returns></returns>
        private async Task PopulateAsync()
        {
            // Get list of skyboxes in background
            var skyboxes = await Task.Run(() =>
            {
                return EnumerateSkyboxFiles();
            });

            // Clear existing list
            foreach (Transform child in this.contentTransform)
            {
                GameObject.Destroy(child.gameObject);
            }

            // Add default entry first
            CreateEntry(Config.Background_Default, Config.Background_Default);

            // Populate list of skyboxes
            foreach (var skybox in skyboxes.OrderBy(key => key.Key))
            {
                CreateEntry(skybox.Key, skybox.Value);
            }
        }

        /// <summary>
        /// Instantiate SkyboxEntry
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        private void CreateEntry(string name, string path)
        {
            var newObj = (GameObject)Instantiate(this.prefabSkyboxEntry, this.contentTransform);
            var entry = newObj.GetComponent<SkyboxEntry>();
            entry.text.text = name;
            entry.path = path;
        }

        /// <summary>
        /// Construct map of backgrond name -> path
        /// </summary>
        /// <returns>Returned map</returns>
        private Dictionary<string, string> EnumerateSkyboxFiles()
        {
            var skyboxes = new Dictionary<string, string>();

            // Enumerate jpg files
            foreach (var filePath in Directory.GetFiles(
                GetOrCreateSkymapPath(), JpgExtSearch))
            {
                skyboxes[Path.GetFileNameWithoutExtension(filePath)] = MakeRelativeSkymapPath(filePath);
            }

            // Enumerate png files
            foreach (var filePath in Directory.GetFiles(
                GetOrCreateSkymapPath(), PngExtSearch))
            {
                skyboxes[Path.GetFileNameWithoutExtension(filePath)] = MakeRelativeSkymapPath(filePath);
            }

            return skyboxes;
        }

        private void EnableBorder(Transform t, bool enable)
        {
            var border = t.Find("Border");
            border?.gameObject.SetActive(enable);
        }

        static private string GetOrCreateSkymapPath()
        {
            string path = Path.Combine(AppConfig.persistentDataPath, SkyboxFolder);
            Directory.CreateDirectory(path);
            return path;
        }

        static private string MakeRelativeSkymapPath(string path)
        {
            return path.Substring(AppConfig.persistentDataPath.Length + 1);
        }

        static private string MakeAbsoluteSkymapPath(string path)
        {
            return Path.Combine(AppConfig.persistentDataPath, path);
        }

        static public bool IsDefaultSkybox(string path)
        {
            return Config.Background_Default.Equals(path, StringComparison.OrdinalIgnoreCase);
        }

        static public string GetSkyboxNameFromPath(string path)
        {
            if (IsDefaultSkybox(path))
            {
                return Config.Background_Default;
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

            return Config.Background_Default;
        }
    }
}