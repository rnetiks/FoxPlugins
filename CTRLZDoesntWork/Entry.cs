using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using CTRLZDoesntWork.KK.Modifiers.GameObject;
using HarmonyLib;
using UnityEngine;

namespace CTRLZDoesntWork.KK
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        const string GUID = "com.fox.ctrlzdoesntwork";
        const string NAME = "CTRL+Z Doesn't Work";
        const string VERSION = "1.0.0";

        public static System.Type[] AvailableModifiers;

        private Harmony _harmony;
        public static ManualLogSource Logger;
        private static GameObject _gameObject;

        private void Awake()
        {
            Logger = base.Logger;
            _gameObject = gameObject;
            var type = typeof(BaseModifier);
            AvailableModifiers = Assembly.GetAssembly(type)
                .GetTypes()
                .Where(e => e.IsClass && !e.IsAbstract && e.IsSubclassOf(type))
                .ToArray()
                .Select(e => e).ToArray();

#if DEBUG
            _gameObject.GetOrAddComponent<MismeshGUI>();
            return;
#endif

            _harmony = Harmony.CreateAndPatchAll(GetType());
        }

        private void OnDestroy()
        {
            foreach (var meshModifier in FindObjectsOfType<MeshModifier>())
            {
                Logger.LogError($"Destroyed {meshModifier.gameObject.name}");
                Destroy(meshModifier);
            }

            Destroy(_gameObject);
            _harmony?.UnpatchSelf();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            _gameObject.GetOrAddComponent<MismeshGUI>();
        }
    }

    internal class MismeshGUI : MonoBehaviour
    {
        private void Awake()
        {
            var items = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToArray();
            foreach (var objectCtrlInfo in items)
            {
                var go = objectCtrlInfo.guideObject.transformTarget.GetComponentsInChildren<MeshFilter>();
                foreach (var meshFilter in go)
                {
                    MeshModifier mesh = meshFilter.GetOrAddComponent<MeshModifier>();
                    
                    /*KILL MEEEEE*/

                    Entry.Logger.LogError($"{mesh.gameObject.name}");
                }

            }
        }
    }
}