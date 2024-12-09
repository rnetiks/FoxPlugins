using System;
using System.Linq;
using DBToggler.Core;
using KKAPI.Studio;
using Studio;
using UnityEngine;

namespace DBToggler
{
    public class ToggleDynamicBones : MonoBehaviour
    {
        private void Update()
        {
            if (Init.SelectAllKey.Value.IsUp())
            {
                var nodes = TreeView.GetAllNodes();
                var treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
                if (treeNodeCtrl.selectNodes.Length < nodes.Length)
                {
                    SelectNodes(nodes);
                    return;
                }

                foreach (var treeNodeObject in treeNodeCtrl.selectNodes)
                {
                    treeNodeCtrl.DeselectNode(treeNodeObject);
                }
                return;
            }

            var selectedCharacters = StudioAPI.GetSelectedCharacters().ToArray();
            if (selectedCharacters.Length > 0)
            {
                if (Init.EnableDynamicBonesKey.Value.IsUp())
                {
                    foreach (var selectedCharacter in selectedCharacters)
                    {
                        var bones = selectedCharacter.charReference.GetComponentsInChildren<DynamicBone>();
                        SetBones(bones, true);
                    }
                }
                else if (Init.DisableDynamicBonesKey.Value.IsUp())
                {
                    foreach (var selectedCharacter in selectedCharacters)
                    {
                        var bones = selectedCharacter.charReference.GetComponentsInChildren<DynamicBone>();                                        
                        SetBones(bones, false);
                    }
                }

                return;
            }

            if (Init.EnableDynamicBonesKey.Value.IsUp())
            {
                var bones = FindObjectsOfType<DynamicBone>();
                SetBones(bones, true);
            }
            else if (Init.DisableDynamicBonesKey.Value.IsUp())
            {
                var bones = FindObjectsOfType<DynamicBone>();
                SetBones(bones, false);
            }
        }

        /// <summary>
        /// Sets the enabled state of the specified DynamicBone instances.
        /// </summary>
        /// <param name="bones">Array of DynamicBone instances to be modified.</param>
        /// <param name="state">Boolean value indicating the desired enabled state of the bones.</param>
        private static void SetBones(DynamicBone[] bones, bool state)
        {
            Init._logger.LogWarning($"{(state ? "Enabling" : "Disabling")} dynamic bones on {bones.Length} bones");
            foreach (var bone in bones)
            {
                bone.enabled = state;
            }
        }

        /// <summary>
        /// Selects the given array of TreeNodeObject nodes in the TreeView.
        /// </summary>
        /// <param name="objects">Array of TreeNodeObject nodes to be selected.</param>
        private static void SelectNodes(TreeNodeObject[] objects)
        {
            if (!StudioAPI.StudioLoaded) return;

            var ctrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
            ctrl.selectNodes = objects;
        }
    }
    
    public class TreeView
    {
        /// <summary>
        /// Retrieves all TreeNodeObject nodes from the TreeView.
        /// </summary>
        /// <returns>
        /// An array of TreeNodeObject nodes. Returns an empty array if Studio is not loaded.
        /// </returns>
        public static TreeNodeObject[] GetAllNodes()
        {
            return StudioAPI.StudioLoaded ? Singleton<Studio.Studio>.Instance.treeNodeCtrl.m_TreeNodeObject.ToArray() : Array.Empty<TreeNodeObject>();
        }
    }
}