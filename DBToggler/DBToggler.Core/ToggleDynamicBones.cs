using System.Linq;
using DBToggler.Core;
using KKAPI.Studio;
using UnityEngine;

namespace DBToggler
{
    public class ToggleDynamicBones : MonoBehaviour
    {
        private void Update()
        {
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
            int n = 0;
            Init._logger.LogWarning($"{(state ? "Enabling" : "Disabling")} dynamic bones on {bones.Length} bones");
            foreach (var bone in bones)
            {
                n++;
                bone.enabled = state;
            }
            
            Init._logger.LogInfo($"Changed state on {n} bones");
        }
    }
}