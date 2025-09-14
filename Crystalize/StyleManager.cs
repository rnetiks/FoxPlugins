using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crystalize
{
    public class StyleManager
    {
        private static readonly Dictionary<string, GUIStyle> _styleCache = new Dictionary<string, GUIStyle>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Retrieves a cached style associated with the specified identifier or creates a new one if it does not exist.
        /// </summary>
        /// <param name="id">The unique identifier for the style.</param>
        /// <param name="callback">An optional callback function to create a new style if it does not exist in the cache.</param>
        /// <returns>
        /// Returns an existing <see cref="GUIStyle"/> associated with the specified identifier,
        /// or a newly created style if none exists. Returns <see cref="GUIStyle.none"/> if no style exists and no callback is provided.
        /// </returns>
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

        /// <summary>
        /// Retrieves a cached style associated with the specified identifier or creates a new one if it does not exist.
        /// </summary>
        /// <param name="id">The unique identifier for the style.</param>
        /// <param name="callback">An optional callback function to create a new style if it does not exist in the cache.</param>
        /// <returns>
        /// Returns an existing <see cref="GUIStyle"/> associated with the specified identifier,
        /// or a newly created style if none exists. Returns <see cref="GUIStyle.none"/> if no style exists and no callback is provided.
        /// </returns>
        public static GUIStyle GetOrCreate<T>(T id, Func<GUIStyle> callback = null) where T : struct
        {
            string key = $"{typeof(T).Name}.{id}";
            return GetOrCreate(key, callback);
        }

        /// <summary>
        /// Removes the cached style associated with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the style to be removed from the cache.</param>
        /// <returns>
        /// Returns <c>true</c> if the style was successfully removed from the cache; otherwise, <c>false</c>.
        /// </returns>
        public static bool Delete(string id)
        {
            lock (_lock)
            {
                return _styleCache.Remove(id);
            }
        }

        /// <summary>
        /// Removes the cached style associated with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the style to be removed from the cache.</param>
        /// <returns>
        /// Returns <c>true</c> if the style was successfully removed from the cache; otherwise, <c>false</c>.
        /// </returns>
        public static bool Delete<T>(T id) where T : struct
        {
            string key = $"{typeof(T).Name}.{id}";
            return Delete(key);
        }

        /// <summary>
        /// Clears all cached styles from the style manager.
        /// </summary>
        /// <remarks>
        /// This method removes all entries from the internal style cache, effectively resetting it.
        /// Any previously created or retrieved styles will no longer be available after this operation.
        /// </remarks>
        public static void Clear()
        {
            lock (_lock)
            {
                _styleCache.Clear();
            }
        }
    }
}