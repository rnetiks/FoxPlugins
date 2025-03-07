using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Guiverload.KKS
{
    [BepInPlugin("com.fox.guiverload", "Guiverload", "1.0.0"), BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        private Harmony patch;
        private static GameObject _gameObject;
        public static ManualLogSource _logSource;
        public static ConfigEntry<float> opacity;
        public static ConfigEntry<KeyboardShortcut> _testMenuKey;

        private void Awake()
        {
            _logSource = Logger;
            opacity = Config.Bind("General", "Window Opacity", .8f);
            _testMenuKey = Config.Bind("Test", "Open Test Menu", new KeyboardShortcut(KeyCode.N));
            opacity.SettingChanged += OpacityOnSettingChanged;
            patch = Harmony.CreateAndPatchAll(GetType());
            _gameObject = gameObject;
#if DEBUG
            _logSource.LogError("StudioScene_Start_Prefix");
            //_gameObject.GetOrAddComponent<Guiverload>();
            _gameObject.GetOrAddComponent<TestWindow>();
#endif
        }

        private void OpacityOnSettingChanged(object sender, EventArgs e)
        {
            var g = _gameObject.GetComponent<Guiverload>();
            g.RecalculateElements();
        }

        private void OnDestroy()
        {
            Destroy(_gameObject);
            patch?.UnpatchSelf();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioScene_Start_Prefix()
        {
            _gameObject.GetOrAddComponent<Guiverload>();
        }
    }
}