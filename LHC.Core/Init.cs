using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LHC.Core
{
    class Init : BaseUnityPlugin
    {
        internal static ConfigEntry<KeyboardShortcut> OpenWindowKey;
        private static GameObject _go;
        
        private void Awake()
        {
            BindKeys(Config);
            Harmony.CreateAndPatchAll(GetType());
        }

        private void OnGUI()
        {
            modal?.OnGUI();
        }

        private Modal modal;

        private static void BindKeys(ConfigFile config)
        {
            OpenWindowKey = config.Bind("Window", "Open", new KeyboardShortcut());
            DynamicBone_Ver02
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
        private readonly Color32 color;

        public RoundedWindow(int width, int height, Color32 color)
        {
            if(width * height > 0xE00000)
                throw new Exception("Autumn is not designed to handle high resolution instances");
            this.color = color;
        }

        private int width, height;

        public byte[] Fill()
        {
            byte[] data = new byte[width * height * 4];
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