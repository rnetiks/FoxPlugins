using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototype
{
    public class Texture
    {
        private static readonly Dictionary<string, CachedTexture> _cache = new Dictionary<string, CachedTexture>();
        private static readonly Dictionary<int, string> _instanceToKey = new Dictionary<int, string>();
    
        public static float CleanupSeconds { get; set; } = 30f;

        private class CachedTexture
        {
            public Texture2D texture;
            public float lastUsed;
            public int referenceCount;
            public string key;
        }

        public static Texture2D GetOrCreate(string key, Func<Texture2D> creator)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                cached.lastUsed = Time.time;
                cached.referenceCount++;
                return cached.texture;
            }

            var texture = creator();
            var cachedTexture = new CachedTexture
            {
                texture = texture,
                lastUsed = Time.time,
                referenceCount = 1,
                key = key
            };

            _cache[key] = cachedTexture;
            _instanceToKey[texture.GetInstanceID()] = key;

            return texture;
        }

        public static Texture2D GetOrCreateSolid(Color color, int width = 1, int height = 1)
        {
            string key = $"solid_{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}_{width}x{height}";

            return GetOrCreate(key, () =>
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = color;
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            });
        }

        public static Texture2D CreateGradient(Color from, Color to, int width = 256, int height = 1, bool horizontal = true)
        {
            string key = $"gradient_{from}_{to}_{width}x{height}_{horizontal}";

            return GetOrCreate(key, () =>
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float t = horizontal ? (float)x / (width - 1) : (float)y / (height - 1);
                        pixels[y * width + x] = Color.Lerp(from, to, t);
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            });
        }

        public static Texture2D CreateRoundedRect(Color color, int width, int height, int cornerRadius)
        {
            string key = $"rounded_{color}_{width}x{height}_r{cornerRadius}";

            return GetOrCreate(key, () =>
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bool inside = IsInsideRoundedRect(x, y, width, height, cornerRadius);
                        pixels[y * width + x] = inside ? color : Color.clear;
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            });
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x < radius && y < radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
            if (x >= width - radius && y < radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius;
            if (x < radius && y >= height - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius;
            if (x >= width - radius && y >= height - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius;

            return true;
        }

        /// <summary>
        /// Release a texture reference by key. Call this when you're done using a texture.
        /// </summary>
        public static bool Release(string key)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                cached.referenceCount = Mathf.Max(0, cached.referenceCount - 1);
                cached.lastUsed = Time.time;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Release a texture reference by texture instance. Call this when you're done using a texture.
        /// </summary>
        public static bool Release(Texture2D texture)
        {
            if (texture == null) return false;
        
            if (_instanceToKey.TryGetValue(texture.GetInstanceID(), out string key))
            {
                return Release(key);
            }
            return false;
        }

        /// <summary>
        /// Check if a texture is cached
        /// </summary>
        public static bool IsTextureCached(string key)
        {
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// Clean up unused textures (reference count = 0 and older than timeout)
        /// </summary>
        public static int CleanupUnused()
        {
            var toRemove = new List<string>();
            float currentTime = Time.time;
            int cleanedCount = 0;

            foreach (var kvp in _cache)
            {
                var cached = kvp.Value;
                if (cached.referenceCount <= 0 && currentTime - cached.lastUsed > CleanupSeconds)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string key in toRemove)
            {
                if (RemoveTextureFromCache(key))
                {
                    cleanedCount++;
                }
            }

            return cleanedCount;
        }

        /// <summary>
        /// Force remove a specific texture from cache (useful for manual cleanup)
        /// </summary>
        public static bool ForceRemove(string key)
        {
            return RemoveTextureFromCache(key);
        }

        private static bool RemoveTextureFromCache(string key)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                if (cached.texture != null)
                {
                    _instanceToKey.Remove(cached.texture.GetInstanceID());
                    UnityEngine.Object.Destroy(cached.texture);
                }
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all cached textures
        /// </summary>
        public static void Clear()
        {
            foreach (var cached in _cache.Values)
            {
                if (cached.texture != null)
                    UnityEngine.Object.Destroy(cached.texture);
            }
            _cache.Clear();
            _instanceToKey.Clear();
        }
    }
}