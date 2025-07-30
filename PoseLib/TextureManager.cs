using System;
using System.Collections.Generic;
using System.IO;
using TexFac.Universal;
using UnityEngine;

namespace PoseLib.KKS
{
    public class TextureManager : IDisposable
    {
        private Texture2D _backgroundTexture;

        public Texture2D GetBackgroundTexture(float width, float height)
        {
            if (_backgroundTexture == null)
                CreateBackgroundTexture(width, height);
            
            return _backgroundTexture;
        }

        public BaseTextureElement CreateScreenshotTexture()
        {
            return TextureFactory.Create(Constants.PREVIEW_SIZE, Constants.PREVIEW_SIZE)
                .BackgroundColor(255, 255, 255, 255);
        }

        public BaseTextureElement ProcessScreenshot(BaseTextureElement screenshot)
        {
            var min = Mathf.Min(screenshot.Width, screenshot.Height);
            var max = Mathf.Max(screenshot.Width, screenshot.Height);

            float x = 0;
            float y = 0;

            if (screenshot.Width > screenshot.Height)
                x = (screenshot.Width - min) / 2;
            else
                y = (screenshot.Height - min) / 2;

            screenshot.Crop(new Rect(x, y, min, min));
            screenshot.Scale(Constants.PREVIEW_SIZE, Constants.PREVIEW_SIZE);
            
            return screenshot;
        }

        public Texture2D CreatePlaceholderTexture(string fileName, string fileExtension)
        {
            var placeholderKey = $"placeholder_{fileExtension}";
            
            if (_placeholderTextures.TryGetValue(placeholderKey, out var cachedPlaceholder))
                return cachedPlaceholder;

            try
            {
                var placeholder = TextureFactory.Create(Constants.PREVIEW_SIZE, Constants.PREVIEW_SIZE)
                    .BackgroundColor(128, 128, 128, 255)
                    .Border(2, Color.black);

                var textColor = fileExtension == ".dat" ? Color.red : Color.blue;
                
                var texture = placeholder.GetTexture();
                
                _placeholderTextures[placeholderKey] = texture;
                return texture;
            }
            catch (Exception)
            {
                return CreateSimplePlaceholder(fileExtension);
            }
        }

        private readonly Dictionary<string, Texture2D> _placeholderTextures = new Dictionary<string, Texture2D>();

        private Texture2D CreateSimplePlaceholder(string fileExtension)
        {
            var texture = new Texture2D(Constants.PREVIEW_SIZE, Constants.PREVIEW_SIZE);
            var fillColor = fileExtension == ".dat" ? Color.red : Color.gray;
            
            var pixels = new Color[Constants.PREVIEW_SIZE * Constants.PREVIEW_SIZE];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fillColor;
                
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }

        private void CreateBackgroundTexture(float width, float height)
        {
            if (!File.Exists(Constants.BACKGROUND_IMAGE_PATH))
            {
                TextureFactory.Create((int)width, (int)height)
                    .BackgroundColor(Constants.BACKGROUND_COLOR.r, Constants.BACKGROUND_COLOR.g, 
                                   Constants.BACKGROUND_COLOR.b, Constants.BACKGROUND_COLOR.a)
                    .Border(2, Constants.BORDER_COLOR)
                    .Opacity(Constants.BACKGROUND_OPACITY)
                    .Save(Constants.BACKGROUND_IMAGE_PATH);
            }
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(File.ReadAllBytes(Constants.BACKGROUND_IMAGE_PATH));
            tex.Apply();
            _backgroundTexture = tex;
        }

        public void Dispose()
        {
            if (_backgroundTexture != null)
            {
                UnityEngine.Object.Destroy(_backgroundTexture);
                _backgroundTexture = null;
            }
            
            foreach (var placeholder in _placeholderTextures.Values)
            {
                if (placeholder != null)
                    UnityEngine.Object.Destroy(placeholder);
            }
            _placeholderTextures.Clear();
        }
    }
}