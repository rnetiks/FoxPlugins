using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Compositor.KK
{
    public static class TextureCache
    {
        private static Stack<Texture2D> _textures = new Stack<Texture2D>();
        private static readonly object _lock = new object();

        public static void AddTexture(Texture2D texture, int maxCacheSize)
        {
            lock (_lock)
            {
                if (_textures.Count >= maxCacheSize)
                {
                    var old = _textures.Pop();
                    if (old != null)
                    {
                        Object.Destroy(old);
                    }
                }
                
                _textures.Push(texture);
            }
        }

        public static Texture2D GetLatestTexture()
        {
            lock (_lock)
            {
                return _textures.Count > 0 ? _textures.Peek() : null;
            }
        }

        public static bool HasTextures()
        {
            lock (_lock)
            {
                return _textures.Count > 0;
            }
        }

        public static int Count
        {
            get
            {
                lock (_lock)
                {
                    return _textures.Count;
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                while (_textures.Count > 0)
                {
                    var texture = _textures.Pop();
                    if (texture != null)
                    {
                        Object.Destroy(texture);
                    }
                }
            }
        }
    }
}