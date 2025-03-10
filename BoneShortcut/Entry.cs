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

        #region FK Binds

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

        #endregion

        #region IK Binds

        public static ConfigEntry<KeyboardShortcut> IK_LEFT_HAND;
        public static ConfigEntry<KeyboardShortcut> IK_RIGHT_HAND;

        public static ConfigEntry<KeyboardShortcut> IK_LEFT_ELBOW;
        public static ConfigEntry<KeyboardShortcut> IK_RIGHT_ELBOW;

        public static ConfigEntry<KeyboardShortcut> IK_LEFT_SHOULDER;
        public static ConfigEntry<KeyboardShortcut> IK_RIGHT_SHOULDER;

        public static ConfigEntry<KeyboardShortcut> IK_STOMACH;
        public static ConfigEntry<KeyboardShortcut> IK_LEFT_WAIST;
        public static ConfigEntry<KeyboardShortcut> IK_RIGHT_WAIST;
        public static ConfigEntry<KeyboardShortcut> IK_LEFT_KNEE;
        public static ConfigEntry<KeyboardShortcut> IK_RIGHT_KNEE;
        public static ConfigEntry<KeyboardShortcut> IK_LEFT_FOOT;
        public static ConfigEntry<KeyboardShortcut> IK_RIGHT_FOOT;

        #endregion

        public static ConfigEntry<bool> Debug;

        private void Awake()
        {
            Debug = Config.Bind("Config", "Debug", false);

            #region FK

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

            #endregion

            IK_LEFT_HAND = Config.Bind("Inverse Kinematics", "Left Hand", KeyboardShortcut.Empty);
            IK_RIGHT_HAND = Config.Bind("Inverse Kinematics", "Right Hand", KeyboardShortcut.Empty);
            IK_LEFT_ELBOW = Config.Bind("Inverse Kinematics", "Left Elbow", KeyboardShortcut.Empty);
            IK_RIGHT_ELBOW = Config.Bind("Inverse Kinematics", "Right Elbow", KeyboardShortcut.Empty);
            IK_LEFT_SHOULDER = Config.Bind("Inverse Kinematics", "Left Shoulder", KeyboardShortcut.Empty);
            IK_RIGHT_SHOULDER = Config.Bind("Inverse Kinematics", "Right Shoulder", KeyboardShortcut.Empty);
            IK_STOMACH = Config.Bind("Inverse Kinematics", "Stomach", KeyboardShortcut.Empty);
            IK_LEFT_WAIST = Config.Bind("Inverse Kinematics", "Left Waist", KeyboardShortcut.Empty);
            IK_RIGHT_WAIST = Config.Bind("Inverse Kinematics", "Right Waist", KeyboardShortcut.Empty);
            IK_LEFT_KNEE = Config.Bind("Inverse Kinematics", "Left Knee", KeyboardShortcut.Empty);
            IK_RIGHT_KNEE = Config.Bind("Inverse Kinematics", "Right Knee", KeyboardShortcut.Empty);
            IK_LEFT_FOOT = Config.Bind("Inverse Kinematics", "Left Foot", KeyboardShortcut.Empty);
            IK_RIGHT_FOOT = Config.Bind("Inverse Kinematics", "Right Foot", KeyboardShortcut.Empty);

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

            var ociChar = _ociChars.First();
            var fkCtrl = ociChar.charReference.transform.GetComponent<FKCtrl>();
            var ikCtrl = ociChar.charReference.transform.GetComponent<IKCtrl>();

            Transform target = ociChar.guideObject.parent;

            if (fkCtrl != null && fkCtrl.enabled)
                HandleFk(target);
            else if (ikCtrl != null && ikCtrl.enabled)
                HandleIK(target);
        }

        private void HandleIK(Transform target)
        {
            var mapping = new Dictionary<KeyboardShortcut, string>()
            {
                {IK_LEFT_HAND.Value, "cf_t_hand_L(work)"},
                {IK_RIGHT_HAND.Value, "cf_t_hand_R(work)"},
                {IK_LEFT_ELBOW.Value, "cf_t_elbo_L(work)"},
                {IK_RIGHT_ELBOW.Value, "cf_t_elbo_R(work)"},
                {IK_LEFT_SHOULDER.Value, "cf_t_shoulder_L(work)"},
                {IK_RIGHT_SHOULDER.Value, "cf_t_shoulder_R(work)"},
                {IK_STOMACH.Value, "cf_t_hips(work)"},
                {IK_LEFT_WAIST.Value, "cf_t_waist_L(work)"},
                {IK_RIGHT_WAIST.Value, "cf_t_waist_R(work)"},
                {IK_LEFT_KNEE.Value, "cf_t_knee_L(work)"},
                {IK_RIGHT_KNEE.Value, "cf_t_knee_R(work)"},
                {IK_LEFT_FOOT.Value, "cf_t_leg_L(work)"},
                {IK_RIGHT_FOOT.Value, "cf_t_leg_R(work)"}
            };

            foreach (var pair in mapping)
            {
                if (!pair.Key.IsDown())
                    continue;
                SelectBone(pair.Value, target);
                break;
            }
        }

        private void HandleFk(Transform target)
        {
            var mapping = new Dictionary<KeyboardShortcut, string>
            {
                { FK_LEFT_WRIST.Value, "cf_j_hand_L" },
                { FK_LEFT_ELBOW.Value, "cf_j_forearm01_L" },
                { FK_LEFT_ARMPIT.Value, "cf_j_arm00_L" },
                { FK_LEFT_SHOULDER.Value, "cf_j_shoulder_L" },
                { FK_RIGHT_WRIST.Value, "cf_j_hand_R" },
                { FK_RIGHT_ELBOW.Value, "cf_j_forearm01_R" },
                { FK_RIGHT_ARMPIT.Value, "cf_j_arm00_R" },
                { FK_RIGHT_SHOULDER.Value, "cf_j_shoulder_R" },
                { FK_NECK.Value, "cf_j_neck" },
                { FK_HEAD.Value, "cf_j_head" },
                { FK_HIPS.Value, "cf_j_hips" },
                { FK_SPINE.Value, "cf_j_spine01" },
                { FK_WAIST.Value, "cf_j_waist01" },
                { FK_TORSO.Value, "cf_j_spine02" },
                { FK_LEFT_THIGH.Value, "cf_j_thigh00_L" },
                { FK_RIGHT_THIGH.Value, "cf_j_thigh00_R" },
                { FK_LEFT_KNEE.Value, "cf_j_leg01_L" },
                { FK_RIGHT_KNEE.Value, "cf_j_leg01_R" },
                { FK_LEFT_FOOT.Value, "cf_j_leg03_L" },
                { FK_RIGHT_FOOT.Value, "cf_j_leg03_R" },
                { FK_LEFT_TOE.Value, "cf_j_toes_L" },
                { FK_RIGHT_TOE.Value, "cf_j_toes_R" }
            };

            foreach (var pair in mapping)
            {
                if (!pair.Key.IsDown())
                    continue;
                SelectBone(pair.Value, target);
                break;
            }
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
            if (Entry.Debug.Value) Entry.Logger.LogWarning($"Set bone to {keyValuePairs.Key}");
            guideObjectManager.selectObject = keyValuePairs.Value;
        }
    }
}