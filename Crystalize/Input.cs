using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Crystalize
{
    public class Input
    {
        private static KeyCode[] SanitizeKeys(params KeyCode[] keys)
        {
            if (keys.Length == 0 || keys[0] == KeyCode.None)
                return new KeyCode[1];
            return new []{ keys[0] }.Concat(keys.Skip(1).Distinct().Where(x => x != keys[0]).OrderBy(x => (int) x)).ToArray();
        }


        /// <summary>
        /// Determines whether the specified keyboard shortcut is currently being pressed down. This is an extension on <see cref="KeyboardShortcut"/> adding support for joystick controls.
        /// </summary>
        /// <param name="bind">The keyboard shortcut to check.</param>
        /// <returns>True if the specified shortcut is pressed down; otherwise, false.</returns>
        public static bool IsDown(KeyboardShortcut bind)
        {
            if (!UnityInput.Current.GetKey(KeyCode.LeftControl) && !bind.MainKey.ToString().ToLower().Contains("joystick"))
                return bind.IsDown();
            KeyCode mainKey = bind.MainKey;
            IEnumerable<KeyCode> source = SanitizeKeys(new []
            {
                mainKey
            }.Concat(bind.Modifiers).ToArray());

            return mainKey != KeyCode.None && UnityInput.Current.GetKeyDown(mainKey) && Mods(source.ToArray(), mainKey);
        }
        
        private static bool Mods(KeyCode[] all, KeyCode main)
        {
            return all.All(c => c == main || UnityInput.Current.GetKey(c));
        }

        /// <summary>
        /// Determines whether the specified key is a modifier key (such as Control, Shift, or Alt).
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the specified key is a modifier key; otherwise, false.</returns>
        public static bool IsModifier(KeyCode key)
        {
            return key == KeyCode.LeftControl || key == KeyCode.RightControl || key == KeyCode.LeftShift || key == KeyCode.RightShift || key == KeyCode.LeftAlt || key == KeyCode.RightAlt;
        }
    }
}