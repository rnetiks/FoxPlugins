using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ColliderSound.KK
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        const string GUID = "com.fox.collidersound";
        const string NAME = "ColliderSound";
        const string VERSION = "1.2";

        public static ManualLogSource _logger;
        private static GameObject bepinex;
        private static Harmony harmony;
        public static ConfigEntry<float> minDistance;
        public static ConfigEntry<float> fadeInTime;
        public static ConfigEntry<KeyboardShortcut> toggleMenu;

        private void Awake()
        {
            minDistance = Config.Bind("Main", "Min Distance", 1f,
                "The minimum an object has to be from another to activate");
            fadeInTime = Config.Bind("Main", "Fade In Time", 0.2f,
                "Increase if you hear popping when you hear popping when the audio starts");
            toggleMenu = Config.Bind("Main", "Toggle Menu", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl));
            _logger = Logger;
            _logger.LogInfo($"Loading ColliderSound[{VERSION}]");
            bepinex = gameObject;
#if DEBUG
            bepinex.GetOrAddComponent<DistanceSound>();
#endif
            harmony = Harmony.CreateAndPatchAll(GetType());
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            bepinex.GetOrAddComponent<DistanceSound>();
        }
    }
}