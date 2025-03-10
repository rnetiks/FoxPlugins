using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Studio;
using Studio;
using UnityEngine;
using static BoneShortcut.Entry;

namespace BoneShortcut
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        const string GUID = "com.fox.boneshortcut";
        const string NAME = "Bone Shortcut";
        const string VERSION = "1.0.0";

        private Harmony _harmony;
        public static ManualLogSource Logger;
        private static GameObject _gameObject;

        public static ConfigEntry<KeyboardShortcut> FK_LEFT_WRIST;
        public static ConfigEntry<KeyboardShortcut> FK_LEFT_ELBOW;
        public static ConfigEntry<KeyboardShortcut> FK_LEFT_ARMPIT;
        public static ConfigEntry<KeyboardShortcut> FK_LEFT_SHOULDER;

        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_WRIST;
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_ELBOW;
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_ARMPIT;
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_SHOULDER;

        public static ConfigEntry<KeyboardShortcut> FK_NECK;
        public static ConfigEntry<KeyboardShortcut> FK_HEAD;

        public static ConfigEntry<KeyboardShortcut> FK_HIPS; // cf_j_hips
        public static ConfigEntry<KeyboardShortcut> FK_SPINE; // cf_j_spine01
        public static ConfigEntry<KeyboardShortcut> FK_WAIST; // cf_j_waist01

        public static ConfigEntry<KeyboardShortcut> FK_TORSO; // cf_j_spine02

        public static ConfigEntry<KeyboardShortcut> FK_LEFT_THIGH; // cf_j_spine01
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_THIGH; // cf_j_spine01

        public static ConfigEntry<KeyboardShortcut> FK_LEFT_KNEE; // cf_j_spine01
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_KNEE; // cf_j_spine01

        public static ConfigEntry<KeyboardShortcut> FK_LEFT_FOOT; // cf_j_spine01
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_FOOT; // cf_j_spine01

        public static ConfigEntry<KeyboardShortcut> FK_LEFT_TOE; // cf_j_spine01
        public static ConfigEntry<KeyboardShortcut> FK_RIGHT_TOE; // cf_j_spine01
        
        public static ConfigEntry<bool> Debug;

        private void Awake()
        {
            Debug = Config.Bind("Config", "Debug", false);
            FK_LEFT_WRIST = Config.Bind("Forward Kinematics", "Left Wrist", KeyboardShortcut.Empty);
            FK_RIGHT_WRIST = Config.Bind("Forward Kinematics", "Right Wrist", KeyboardShortcut.Empty);
            FK_LEFT_ELBOW = Config.Bind("Forward Kinematics", "Left Elbow", KeyboardShortcut.Empty);
            FK_RIGHT_ELBOW = Config.Bind("Forward Kinematics", "Right Elbow", KeyboardShortcut.Empty);
            FK_LEFT_ARMPIT = Config.Bind("Forward Kinematics", "Left Armpit", KeyboardShortcut.Empty);
            FK_RIGHT_ARMPIT = Config.Bind("Forward Kinematics", "Right Armpit", KeyboardShortcut.Empty);
            FK_LEFT_SHOULDER = Config.Bind("Forward Kinematics", "Left Shoulder", KeyboardShortcut.Empty);
            FK_RIGHT_SHOULDER = Config.Bind("Forward Kinematics", "Right Shoulder", KeyboardShortcut.Empty);

            FK_NECK = Config.Bind("Forward Kinematics", "Neck", KeyboardShortcut.Empty);
            FK_HEAD = Config.Bind("Forward Kinematics", "Head", KeyboardShortcut.Empty);
            FK_HIPS = Config.Bind("Forward Kinematics", "Hips", KeyboardShortcut.Empty);
            FK_SPINE = Config.Bind("Forward Kinematics", "Spine", KeyboardShortcut.Empty);
            FK_WAIST = Config.Bind("Forward Kinematics", "Waist", KeyboardShortcut.Empty);
            FK_TORSO = Config.Bind("Forward Kinematics", "Torso", KeyboardShortcut.Empty);

            FK_LEFT_THIGH = Config.Bind("Forward Kinematics", "Left Thigh", KeyboardShortcut.Empty);
            FK_RIGHT_THIGH = Config.Bind("Forward Kinematics", "Right Thigh", KeyboardShortcut.Empty);

            FK_LEFT_KNEE = Config.Bind("Forward Kinematics", "Left Knee", KeyboardShortcut.Empty);
            FK_RIGHT_KNEE = Config.Bind("Forward Kinematics", "Right Knee", KeyboardShortcut.Empty);

            FK_LEFT_FOOT = Config.Bind("Forward Kinematics", "Left Foot", KeyboardShortcut.Empty);
            FK_RIGHT_FOOT = Config.Bind("Forward Kinematics", "Right Foot", KeyboardShortcut.Empty);

            FK_LEFT_TOE = Config.Bind("Forward Kinematics", "Left Toe", KeyboardShortcut.Empty);
            FK_RIGHT_TOE = Config.Bind("Forward Kinematics", "Right Toe", KeyboardShortcut.Empty);

            Logger = base.Logger;
            Logger.LogWarning($"{NAME} is loaded!");
            _gameObject = gameObject;
            //_gameObject.GetOrAddComponent<BoneShortcutHandler>();
            _harmony = Harmony.CreateAndPatchAll(GetType());
        }

        private void OnDestroy()
        {
            Destroy(_gameObject);
            _harmony?.UnpatchSelf();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            _gameObject.GetOrAddComponent<BoneShortcutHandler>();
        }
    }

    internal class BoneShortcutHandler : MonoBehaviour
    {
        private void Update()
        {
            var _ociChars = StudioAPI.GetSelectedCharacters().ToArray();
            if (_ociChars.Length <= 0)
                return;

            var fkCtrl = _ociChars.First().charReference.transform.GetComponent<FKCtrl>();
            var ikCtrl = _ociChars.First().charReference.transform.GetComponent<IKCtrl>();

            Transform target = _ociChars.First().guideObject.parent;

            if (fkCtrl != null && fkCtrl.enabled)
                HandleFK(target);
        }

        private void HandleFK(Transform target)
        {
            if (FK_LEFT_WRIST.Value.IsDown())
                SelectBone("cf_j_hand_L", target);
            else if (FK_LEFT_ELBOW.Value.IsDown())
                SelectBone("cf_j_forearm01_L", target);
            else if (FK_LEFT_ARMPIT.Value.IsDown())
                SelectBone("cf_j_arm00_L", target);
            else if (FK_LEFT_SHOULDER.Value.IsDown())
                SelectBone("cf_j_shoulder_L", target);
            else if (FK_RIGHT_WRIST.Value.IsDown())
                SelectBone("cf_j_hand_R", target);
            else if (FK_RIGHT_ELBOW.Value.IsDown())
                SelectBone("cf_j_forearm01_R", target);
            else if (FK_RIGHT_ARMPIT.Value.IsDown())
                SelectBone("cf_j_arm00_R", target);
            else if (FK_RIGHT_SHOULDER.Value.IsDown())
                SelectBone("cf_j_shoulder_R", target);
            else if (FK_NECK.Value.IsDown())
                SelectBone("cf_j_neck", target);
            else if (FK_HEAD.Value.IsDown())
                SelectBone("cf_j_head", target);
            else if (FK_HIPS.Value.IsDown())
                SelectBone("cf_j_hips", target);
            else if (FK_SPINE.Value.IsDown())
                SelectBone("cf_j_spine01", target);
            else if (FK_WAIST.Value.IsDown())
                SelectBone("cf_j_waist01", target);
            else if (FK_TORSO.Value.IsDown())
                SelectBone("cf_j_spine02", target);
            else if (FK_LEFT_THIGH.Value.IsDown())
                SelectBone("cf_j_thigh00_L", target);
            else if (FK_RIGHT_THIGH.Value.IsDown())
                SelectBone("cf_j_thigh00_R", target);
            else if (FK_LEFT_KNEE.Value.IsDown())
                SelectBone("cf_j_leg01_L", target);
            else if (FK_RIGHT_KNEE.Value.IsDown())
                SelectBone("cf_j_leg01_R", target);
            else if (FK_LEFT_FOOT.Value.IsDown())
                SelectBone("cf_j_leg03_L", target);
            else if (FK_RIGHT_FOOT.Value.IsDown())
                SelectBone("cf_j_leg03_R", target);
            else if (FK_LEFT_TOE.Value.IsDown())
                SelectBone("cf_j_toes_L", target);
            else if (FK_RIGHT_TOE.Value.IsDown())
                SelectBone("cf_j_toes_R", target);
        }

        public GuideObject TargetObject()
        {
            GuideObject guideObject = Singleton<GuideObjectManager>.Instance.operationTarget;
            if (guideObject == null)
                guideObject = Singleton<GuideObjectManager>.Instance.selectObject;
            return guideObject;
        }

        private void SelectBone(string bone, Transform target)
        {
            var guideObjectManager = Singleton<GuideObjectManager>.Instance;
            var instanceDicGuideObject = guideObjectManager.dicGuideObject;
            var keyValuePairs = instanceDicGuideObject.First(e => e.Key.name == bone && e.Value.parent == target);
            if(Entry.Debug.Value) Entry.Logger.LogWarning($"Set bone to {keyValuePairs.Key}");
            guideObjectManager.selectObject = keyValuePairs.Value;
        }
    }
}