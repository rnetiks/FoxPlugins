using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace MoreBinds.KKS
{
    [BepInProcess("CharaStudio"), BepInPlugin(GUID, NAME, VERSION)]
    public class Entry : BaseUnityPlugin
    {
        public const string GUID = "com.fox.morebinds";
        public const string NAME = "MoreBinds";
        public const string VERSION = "1.0.0";
        private static ConfigEntry<KeyboardShortcut> _hideKey;
        private static ConfigEntry<KeyboardShortcut> _unhideKey;

        private void Awake()
        {
            _hideKey = Config.Bind("Mesh", "Hide", new KeyboardShortcut(KeyCode.H));
            _unhideKey = Config.Bind("Mesh", "Unhide", new KeyboardShortcut(KeyCode.H, KeyCode.LeftAlt));
        }

        private void Update()
        {
            HandleInput();
        }

        // Store original bone weights for restoration
        private Dictionary<SkinnedMeshRenderer, BoneWeight[]> _originalBoneWeights = new Dictionary<SkinnedMeshRenderer, BoneWeight[]>();

        private void HandleInput()
        {
            if (_hideKey.Value.IsDown())
            {
                var manager = Singleton<GuideObjectManager>.Instance;
                
                foreach (var o in manager.hashSelectObject)
                {
                    Transform boneTransform = o.transformTarget;
                    
                    
                    
                    Logger.LogInfo($"Selected bone: {boneTransform.name}");

                    // Get all bones in the hierarchy under the selected bone (including itself)
                    var allBones = new HashSet<Transform>();
                    allBones.Add(boneTransform);
                    GetAllChildren(boneTransform, allBones);
                    
                    Logger.LogInfo($"Total bones in hierarchy: {allBones.Count}");

                    var chaControl = boneTransform.GetComponentInParent<ChaControl>();
                    if (chaControl == null) continue;

                    var smrs = chaControl.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var smr in smrs)
                    {
                        // Find bone indices that we want to hide
                        var boneIndicesToHide = new List<int>();
                        for (int i = 0; i < smr.bones.Length; i++)
                        {
                            if (allBones.Contains(smr.bones[i]))
                            {
                                boneIndicesToHide.Add(i);
                            }
                        }

                        if (boneIndicesToHide.Count > 0)
                        {
                            Logger.LogInfo($"Hiding vertices in mesh: {smr.name} (bone indices: {string.Join(", ", boneIndicesToHide)})");
                            HideVerticesForBones(smr, boneIndicesToHide);
                        }
                    }

                }
            }

            if (_unhideKey.Value.IsDown())
            {
                foreach (var kvp in _originalBoneWeights)
                {
                    var smr = kvp.Key;
                    var originalWeights = kvp.Value;
                    
                    if (smr != null)
                    {
                        var mesh = smr.sharedMesh;
                        mesh.boneWeights = originalWeights;
                        smr.sharedMesh = mesh;
                        Logger.LogInfo($"Restored mesh: {smr.name}");
                    }
                }
                _originalBoneWeights.Clear();
            }
        }

        private void HideVerticesForBones(SkinnedMeshRenderer smr, List<int> boneIndices)
        {
            var mesh = smr.sharedMesh;
            
            // Store original bone weights if not already stored
            if (!_originalBoneWeights.ContainsKey(smr))
            {
                _originalBoneWeights[smr] = mesh.boneWeights;
            }

            // Get current bone weights
            var boneWeights = mesh.boneWeights;
            var newBoneWeights = new BoneWeight[boneWeights.Length];

            // Zero out weights for vertices affected by the selected bones
            for (int i = 0; i < boneWeights.Length; i++)
            {
                var bw = boneWeights[i];
                var newBw = new BoneWeight();

                // Check each bone influence and zero it out if it's in our hide list
                if (!boneIndices.Contains(bw.boneIndex0))
                {
                    newBw.boneIndex0 = bw.boneIndex0;
                    newBw.weight0 = bw.weight0;
                }
                if (!boneIndices.Contains(bw.boneIndex1))
                {
                    newBw.boneIndex1 = bw.boneIndex1;
                    newBw.weight1 = bw.weight1;
                }
                if (!boneIndices.Contains(bw.boneIndex2))
                {
                    newBw.boneIndex2 = bw.boneIndex2;
                    newBw.weight2 = bw.weight2;
                }
                if (!boneIndices.Contains(bw.boneIndex3))
                {
                    newBw.boneIndex3 = bw.boneIndex3;
                    newBw.weight3 = bw.weight3;
                }

                newBoneWeights[i] = newBw;
            }

            // Apply modified bone weights
            mesh.boneWeights = newBoneWeights;
            smr.sharedMesh = mesh;
        }

        // Helper method to recursively get all child transforms
        private void GetAllChildren(Transform parent, HashSet<Transform> collection)
        {
            foreach (Transform child in parent)
            {
                collection.Add(child);
                GetAllChildren(child, collection);
            }
        }
    }
}