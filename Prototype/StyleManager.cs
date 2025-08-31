using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototype
{
    public class StyleManager
    {
        private static readonly Dictionary<string, GUIStyle> _styleCache = new Dictionary<string, GUIStyle>();
        private static readonly object _lock = new object();

        public static GUIStyle GetOrCreate(string id, Func<GUIStyle> callback = null)
        {
            lock (_lock)
            {
                if (_styleCache.TryGetValue(id, out var style))
                {
                    return style;
                }

                if (callback == null)
                {
                    return GUIStyle.none;
                }

                style = callback();
                _styleCache[id] = style;
                return style;
            }
        }

        public static GUIStyle GetOrCreate<T>(T id, Func<GUIStyle> callback = null) where T : struct
        {
            string key = $"{typeof(T).Name}.{id}";
            return GetOrCreate(key, callback);
        }

        public static bool Delete(string id)
        {
            lock (_lock)
            {
                return _styleCache.Remove(id);
            }
        }
        
        public static bool Delete<T>(T id) where T : struct
        {
            string key = $"{typeof(T).Name}.{id}";
            return Delete(key);
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _styleCache.Clear();
            }
        }
    }
}