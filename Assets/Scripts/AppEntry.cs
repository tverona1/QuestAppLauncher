using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuestAppLauncher
{
    public class AppEntry : MonoBehaviour
    {
        // Sprite gameobject
        public GameObject sprite = null;
        public Image image = null;
        public AspectRatioFitter aspectRatioFitter = null;

        // TMP text
        public TextMeshProUGUI text;

        // Scroll view transform
        public RectTransform scrollViewTransform;

        // App entry contents
        public string packageId;
        public string appName;
        public string externalIconPath = null;
        public int installedApkIndex = -1;
        public bool isRenameMode;
        public bool dynamicallyLoadIcon;

        const int MAX_FRAME_DELAYS = 35;
        private float lastPos = -1f;
        private bool isIconLoaded = false;
        private int curFrameDelay = 0;
        private int maxFrameDelays = 0;
        private float maxDistance = 0f;
        private System.Random rand = new System.Random();

        // Start is called before the first frame update
        void Start()
        {
            // Calculate max distance outside the visible scroll area, beyond which we'll unload the icon to reduce memory
            this.maxDistance = 1.5f * this.scrollViewTransform.rect.size.y;

            // Calculate the maximum frame delays when populating this icon; used to stagger loading of icons in the visible scroll area
            this.maxFrameDelays = this.rand.Next(MAX_FRAME_DELAYS);
        }

        // Update is called once per frame
        async void Update()
        {
            if (!this.dynamicallyLoadIcon || null == this.scrollViewTransform ||
                (this.externalIconPath == null && this.installedApkIndex == -1))
            {
                return;
            }

            var localPos = this.scrollViewTransform.InverseTransformPoint(this.transform.position).y;
            if (Mathf.Abs(localPos) < this.maxDistance)
            {
                // Don't bother loading icons while we're scrolling
                if (this.lastPos == -1)
                {
                    this.lastPos = localPos;
                    return;
                }

                if (this.lastPos != localPos)
                {
                    this.lastPos = localPos;
                    return;

                }

                // We're within max distance, so show the icon after some delay (to stagger loading of icons)
                if (this.curFrameDelay < this.maxFrameDelays)
                {
                    this.curFrameDelay++;
                    return;
                }
                this.maxFrameDelays = this.rand.Next(MAX_FRAME_DELAYS);
                this.curFrameDelay = 0;

                if (!this.isIconLoaded)
                {
                    UnloadIcon();
                    await LoadIcon();
                    this.isIconLoaded = true;
                }
            }
            else if (this.isIconLoaded)
            {
                // We're outside max distance, so unload the icon
                UnloadIcon();
                this.isIconLoaded = false;
            }
        }

        public async Task LoadIcon()
        {
            var result = await AppProcessor.GetAppIconAsync(this.externalIconPath, this.installedApkIndex, 1024 * 1024);
            var image = result.Item1;
            var imageWidth = result.Item2;
            var imageHeight = result.Item3;

            Texture2D texture = null;

            if (null == image)
            {
                Debug.LogFormat("Error loading icon: Path: {0}, Index: {1}", this.externalIconPath, this.installedApkIndex);
                return;
            }

            // Set the icon image
            if (imageWidth == 0 || imageHeight == 0)
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 16;
                texture.LoadImage(image);
            }
            else
            {
                texture = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 16;
                texture.LoadRawTextureData(image);
                texture.Apply();
            }

            var rect = new Rect(0, 0, texture.width, texture.height);
            this.image.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
            this.image.color = Color.white;

            // Preserve icon's aspect ratio
            this.aspectRatioFitter.aspectRatio = (float)texture.width / (float)texture.height;
        }

        private void UnloadIcon()
        {
            // We always manually create the sprite's texture object,
            // so we must explicitly destroy it.
            if (null != this.image.sprite)
            {
                if (null != this.image.sprite.texture)
                {
                    DestroyImmediate(image.sprite.texture);
                }

                DestroyImmediate(this.image.sprite);
                this.image.sprite = null;
                this.image.color = Color.clear;
            }
        }

        private void OnDestroy()
        {
            Debug.Log("AppEntry.OnDestroy");
            UnloadIcon();
        }
    }
}