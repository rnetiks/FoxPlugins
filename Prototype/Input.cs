using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Prototype
{
    public class Input
    {
        private static KeyCode[] SanitizeKeys(params KeyCode[] keys)
        {
            if (keys.Length == 0 || keys[0] == KeyCode.None)
                return new KeyCode[1];
            return ((IEnumerable<KeyCode>) new KeyCode[1]{ keys[0] }).Concat<KeyCode>((IEnumerable<KeyCode>) ((IEnumerable<KeyCode>) keys).Skip<KeyCode>(1).Distinct<KeyCode>().Where<KeyCode>((Func<KeyCode, bool>) (x => x != keys[0])).OrderBy<KeyCode, int>((Func<KeyCode, int>) (x => (int) x))).ToArray<KeyCode>();
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
            IEnumerable<KeyCode> source = (IEnumerable<KeyCode>) SanitizeKeys(((IEnumerable<KeyCode>) new KeyCode[1]
            {
                mainKey
            }).Concat<KeyCode>(bind.Modifiers).ToArray<KeyCode>());

            return mainKey != KeyCode.None && UnityInput.Current.GetKeyDown(mainKey) && Mods(source.ToArray<KeyCode>(), mainKey);
        }
        
        private static bool Mods(KeyCode[] all, KeyCode main)
        {
            return ((IEnumerable<KeyCode>) all).All<KeyCode>((Func<KeyCode, bool>) (c => c == main || UnityInput.Current.GetKey(c)));
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