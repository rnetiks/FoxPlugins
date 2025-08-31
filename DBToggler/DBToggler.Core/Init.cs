using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Diagnostics;

namespace DBToggler.Core
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("CharaStudio")]
    public class Init : BaseUnityPlugin
    {
        private const string GUID = "fox.dbtoggler";
        private const string NAME = "Dynamic Bone Toggler";
        private const string VERSION = "1.0.0.0";

        internal static ConfigEntry<KeyboardShortcut> EnableDynamicBonesKey;
        internal static ConfigEntry<KeyboardShortcut> DisableDynamicBonesKey;
        internal static ConfigEntry<KeyboardShortcut> ExperimanlBonesKey;
        internal static ManualLogSource _logger;

        private static GameObject bepinex;
        private static Harmony harmony;

        private void Awake()
        {
            EnableDynamicBonesKey = Config.Bind("Bones", "Enable", new KeyboardShortcut(KeyCode.G), "Partial support");
            DisableDynamicBonesKey =
                Config.Bind("Bones", "Disable", new KeyboardShortcut(KeyCode.H), "Partial support");
            ExperimanlBonesKey = Config.Bind("Experimental", "Toggle", KeyboardShortcut.Empty,
                "Experimental toggle that keeps the current position of the bones rather than resetting it");
            _logger = Logger;
            bepinex = gameObject;
            harmony = Harmony.CreateAndPatchAll(GetType());
            // bepinex.GetOrAddComponent<ToggleDynamicBones>();
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        public static bool EnableDynamicBones = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.LateUpdate))]
        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.Update))]
        [HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.LateUpdate))]
        [HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.Update))]
        public static bool DynamicBonePatch(DynamicBone __instance)
        {
            return EnableDynamicBones;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            bepinex.GetOrAddComponent<ToggleDynamicBones>();
        }
    }
}