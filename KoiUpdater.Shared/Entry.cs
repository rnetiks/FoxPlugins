using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Manager;
using Studio;
using UnityEngine;

namespace KoiUpdater.Shared
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        private Harmony harmony;
        private const string Guid = "com.fox.kpu", Name = "KPU", Version = "1.0.0";
        internal static ConfigEntry<bool> _autoUpdate;
        internal static ConfigEntry<KeyboardShortcut> _openUI;
        internal static ConfigEntry<string> _serverUrl;
        public static ManualLogSource _logger;
        private static GameObject _go;


        private void Awake()
        {
            _autoUpdate = Config.Bind("General", "Auto Update", false,
                "If the plugin should automatically attempt to update all plugins upon each restart (This can lead to a negative experience)");
            _openUI = Config.Bind("General", "Open UI", new KeyboardShortcut(KeyCode.P, KeyCode.LeftShift), "Open UI");
            _serverUrl = Config.Bind("Advanced", "Server URL", "https://rnetiks.com/", "The URL of the server to connect to, only change this is you are entirely sure on what you are doing.");
            _logger = Logger;
            _go = gameObject;
            harmony = Harmony.CreateAndPatchAll(GetType());
            if (KKAPI.Studio.StudioAPI.StudioLoaded)
                _go.GetOrAddComponent<KoiUpdaterUI>();
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            _go.GetOrAddComponent<KoiUpdaterUI>();
        }
        
    }
};