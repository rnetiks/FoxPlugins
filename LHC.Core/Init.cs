using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace LHC.Core
{
    class Init : BaseUnityPlugin
    {
        internal static ConfigEntry<KeyboardShortcut> OpenWindowKey;
        private static GameObject _go = new();
        
        private void Awake()
        {
            BindKeys(Config);
            Harmony.CreateAndPatchAll(GetType());
        }

        private void OnGUI()
        {
            Render();

        }

        private Action Render = () => {};

        private static void BindKeys(ConfigFile config)
        {
            OpenWindowKey = config.Bind("Window", "Open", new KeyboardShortcut());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        public static void Entry()
        {
            foreach (var constructorInfo in typeof(MonoBehaviour).GetConstructors())
            {
                constructorInfo.Invoke(Array.Empty<object>());
            }

            _go.GetOrAddComponent<LHCWindow>();
        }
    }

    unsafe class RoundedWindow
    {

        public RoundedWindow(int width, int height)
        {
            this._width = width;
            this._height = height;
        }
        private readonly Color32 color;

        public RoundedWindow(int width, int height, Color32 color)
        {
            if(width * height > 0xE00000)
                throw new Exception("Autumn is not designed to handle high resolution instances");
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            _width = width;
            _height = height;
            this.color = color;
        }

        private readonly int _width;
        private readonly int _height;

        public byte[] Fill()
        {
            byte[] data = new byte[_width * _height * 4];
            fixed (byte* ptr = data)
            {
                for (int i = 0; i < data.Length / 4; i+=4)
                {
                    ptr[i*4 + 0] = color.r;
                    ptr[i*4 + 1] = color.g;
                    ptr[i*4 + 2] = color.b;
                    ptr[i*4 + 3] = color.a;
                }
            }
            return data;
        }
    }
}