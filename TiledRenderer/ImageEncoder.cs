using System;
using System.Reflection;
using UnityEngine;

namespace TiledRenderer
{
    internal static class ImageEncoder
    {
        private static MethodInfo _modernPNG;
        private static MethodInfo _modernJPG;
        private static MethodInfo _legacyPNG;
        private static MethodInfo _legacyJPG;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                Type imageConversion = Type.GetType("UnityEngine.ImageConversion") ??
                                       Array.Find(AppDomain.CurrentDomain.GetAssemblies(), 
                                               a => a.GetType("UnityEngine.ImageConversion") != null)
                                           ?.GetType("UnityEngine.ImageConversion");

                if (imageConversion != null)
                {
                    _modernPNG = imageConversion.GetMethod("EncodeToPNG", 
                        BindingFlags.Public | BindingFlags.Static, null, 
                        new[] { typeof(Texture2D) }, null);
                    _modernJPG = imageConversion.GetMethod("EncodeToJPG", 
                        BindingFlags.Public | BindingFlags.Static, null, 
                        new[] { typeof(Texture2D) }, null);
                }

                _legacyPNG = typeof(Texture2D).GetMethod("EncodeToPNG", 
                    BindingFlags.Public | BindingFlags.Instance, null, 
                    Type.EmptyTypes, null);
                _legacyJPG = typeof(Texture2D).GetMethod("EncodeToJPG", 
                    BindingFlags.Public | BindingFlags.Instance, null, 
                    Type.EmptyTypes, null);
            }
            catch { }

            _initialized = true;
        }

        public static byte[] EncodeToPNG(Texture2D texture)
        {
            if (!_initialized) Initialize();

            if (_modernPNG != null)
                return (byte[])_modernPNG.Invoke(null, new object[] { texture });
            if (_legacyPNG != null)
                return (byte[])_legacyPNG.Invoke(texture, new object[0]);

            throw new NotSupportedException("PNG encoding not available");
        }

        public static byte[] EncodeToJPG(Texture2D texture)
        {
            if (!_initialized) Initialize();

            if (_modernJPG != null)
                return (byte[])_modernJPG.Invoke(null, new object[] { texture });
            if (_legacyJPG != null)
                return (byte[])_legacyJPG.Invoke(texture, new object[0]);

            throw new NotSupportedException("JPG encoding not available");
        }
    }
}