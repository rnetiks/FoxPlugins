using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using UnityEngine;

namespace KK
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        private const string GUID = "com.fox.CIO";
        private const string NAME = "CIO";
        private const string VERSION = "1.1.0";
        
        public static ManualLogSource Logger;
        private static GameObject go;
        private Harmony harmony;
        public static ConfigEntry<KeyboardShortcut> enableGUI;

        private void Awake()
        {
            enableGUI = Config.Bind("General", "Enable GUI", new KeyboardShortcut(KeyCode.None));
            Logger = base.Logger;
            go = gameObject;
            go.AddComponent<CIO>();
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>("com.fox.CIO");
        }

        private void OnDestroy()
        {
            StudioSaveLoadApi.UnregisterBehaviour<SceneController>();
            Destroy(go);
            harmony?.UnpatchSelf();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof (StudioScene), "Start")]
        private static void CIO() => go.AddComponent<CIO>();
    }
}