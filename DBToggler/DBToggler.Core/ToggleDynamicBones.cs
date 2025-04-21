using System.Collections.Generic;
using System.Linq;
using DBToggler.Core;
using KKAPI.Studio;
using Studio;
using UnityEngine;

namespace DBToggler
{
    /// <summary>
    /// Component responsible for enabling/disabling dynamic bones based on keyboard input.
    /// </summary>
    public class ToggleDynamicBones : MonoBehaviour
    {
        private void Update()
        {
            bool enableRequested = Init.EnableDynamicBonesKey.Value.IsUp();
            bool disableRequested = Init.DisableDynamicBonesKey.Value.IsUp();

            if (!enableRequested && !disableRequested)
                return;
                
            bool targetState = enableRequested;

            var selectedCharacters = StudioAPI.GetSelectedCharacters().ToArray();
            
            if (selectedCharacters.Length > 0)
            {
                ToggleBonesOnSelectedCharacters(selectedCharacters, targetState);
            }
            else
            {
                ToggleAllBonesInScene(targetState);
            }
        }
        
        /// <summary>
        /// Toggle dynamic bones on all selected characters.
        /// </summary>
        /// <param name="characters">Array of selected characters.</param>
        /// <param name="state">Target state for dynamic bones.</param>
        private void ToggleBonesOnSelectedCharacters(IReadOnlyCollection<OCIChar> characters, bool state)
        {
            Init._logger.LogWarning($"{(state ? "Enabling" : "Disabling")} dynamic bones on {characters.Count} character(s)");
            
            foreach (var character in characters)
            {
                var bones = character.charReference.GetComponentsInChildren<DynamicBone>();
                SetBones(bones, state);
            }
        }
        
        /// <summary>
        /// Toggle all dynamic bones in the scene.
        /// </summary>
        /// <param name="state">Target state for dynamic bones.</param>
        private void ToggleAllBonesInScene(bool state)
        {
            var bones = FindObjectsOfType<DynamicBone>();
            SetBones(bones, state);
        }

        /// <summary>
        /// Sets the enabled state of the specified DynamicBone instances.
        /// </summary>
        /// <param name="bones">Array of DynamicBone instances to be modified.</param>
        /// <param name="state">Boolean value indicating the desired enabled state of the bones.</param>
        private static void SetBones(IReadOnlyCollection<DynamicBone> bones, bool state)
        {
            if (bones.Count == 0)
            {
                Init._logger.LogInfo("No dynamic bones found to modify");
                return;
            }
            
            Init._logger.LogWarning($"{(state ? "Enabling" : "Disabling")} {bones.Count} dynamic bones");
            
            foreach (var bone in bones)
            {
                bone.enabled = state;
            }
            
            Init._logger.LogInfo($"Changed state on {bones.Count} bones");
        }
    }
}