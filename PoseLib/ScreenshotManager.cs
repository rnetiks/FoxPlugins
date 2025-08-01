using System;
using UnityEngine;
using BepInEx.Logging;

namespace PoseLib.KKS
{
    public class ScreenshotManager
    {
        private readonly ManualLogSource _logger;
        private Camera _screenshotCamera;

        public ScreenshotManager(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Takes a screenshot of the current view and returns it as a Texture2D
        /// </summary>
        /// <param name="width">Width of the screenshot</param>
        /// <param name="height">Height of the screenshot</param>
        /// <param name="fixAspectRatio">Whether to crop to 1:1 aspect ratio</param>
        /// <returns>Screenshot as Texture2D</returns>
        public Texture2D TakeScreenshot(int width = 256, int height = 256, bool fixAspectRatio = true)
        {
            try
            {
                var camera = GetActiveCamera();
                if (camera == null)
                {
                    _logger.LogError("No active camera found for screenshot");
                    return CreateErrorTexture(width, height);
                }

                var renderTexture = new RenderTexture(width, height, 24);
                var previousTarget = camera.targetTexture;
                var previousActive = RenderTexture.active;

                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;

                camera.Render();

                var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;

                UnityEngine.Object.DestroyImmediate(renderTexture);

                if (fixAspectRatio && width != height)
                {
                    screenshot = FixAspectRatio(screenshot);
                }

                return screenshot;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to take screenshot: {ex.Message}");
                return CreateErrorTexture(width, height);
            }
        }

        /// <summary>
        /// Crops a texture to 1:1 aspect ratio using the smaller dimension
        /// </summary>
        private Texture2D FixAspectRatio(Texture2D source)
        {
            if (source.width == source.height)
                return source;

            var size = Mathf.Min(source.width, source.height);
            var x = (source.width - size) / 2;
            var y = (source.height - size) / 2;

            var pixels = source.GetPixels(x, y, size, size);
            var croppedTexture = new Texture2D(size, size, source.format, false);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();

            return croppedTexture;
        }

        private Camera GetActiveCamera()
        {
            var studioCamera = Camera.main;
            if (studioCamera != null)
                return studioCamera;

            var cameras = Camera.allCameras;
            foreach (var camera in cameras)
            {
                if (camera.isActiveAndEnabled)
                    return camera;
            }

            return null;
        }

        private Texture2D CreateErrorTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var pixels = new Color[width * height];

            var errorColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = errorColor;

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public void Dispose()
        {
            if (_screenshotCamera != null)
            {
                UnityEngine.Object.Destroy(_screenshotCamera);
                _screenshotCamera = null;
            }
        }
    }
}