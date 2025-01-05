using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MessagePack;
using ParadoxNotion.Serialization;
using Sirenix.Serialization;
using UnityEngine;

namespace KoiUpdater.Shared
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        private const string Guid = "com.fox.pluginupdater", Name = "Plugin Updater", Version = "1.0.0";
        private static ConfigEntry<bool> _autoUpdate;
        private static ConfigEntry<KeyboardShortcut> _openUI;

        
        private void Awake()
        {
            _autoUpdate = Config.Bind("General", "Auto Update", false, "If the plugin should automatically attempt to update all plugins upon each restart (This can lead to a negative experience)");
            _openUI = Config.Bind("General", "Open UI", new KeyboardShortcut(KeyCode.P, KeyCode.LeftShift), "Open UI");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            if (_autoUpdate.Value)
            {
                var jsonDataWriter = new JsonDataWriter();
                JSONSerializer.Serialize(typeof(Entry), null);
            }
        }
    }
};

