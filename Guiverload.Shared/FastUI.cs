using System;
using System.Collections.Generic;
using SmartRectV0;
using UnityEngine;

namespace Guiverload.KKS
{
    abstract class FastUI
    {
        public static void TextField(ref string text, Rect position)
        {
            text = GUI.TextField(position, text);
        }

        public static void TextField(ref string text, Rect position, GUIStyle style)
        {
            text = GUI.TextField(position, text, style);
        }

        public static void Button(Rect position, string text, Action action)
        {
            if (GUI.Button(position, text))
                action();
        }

        public static SmartRect minSize = new SmartRect(0f, 0f, 100f, 20f);
        public static void Spoiler(SmartRect position, string text, Action action, ref bool isOpen)
        {
            var open = isOpen;
            var rect = new Rect(position.X, position.Y, position.Width, minSize.Height);
            if (!isOpen)
            {
                Button(rect, "\u2193 " + text, () => open = true);
            }
            else
            {
                Button(rect, "\u2191 " + text, () => open = false);
                action();
            }

            isOpen = open;
        }

        /// <summary>
        /// Calculate the visible items from a Unity scrollview given it's current scroll position, and the size each element takes up
        /// From the min-2 index of visible, to the max+2 visible one, while considering the scrollviews own height
        /// </summary>
        public static KeyValuePair<int, int> GetVisibleFields(Vector2 scrollPosition, int elementHeight,
            int scrollHeight, int max)
        {
            if (elementHeight <= 0 || max <= 0)
                return new KeyValuePair<int, int>(0, 0);

            int firstVisible = Mathf.Max((int)(scrollPosition.y / elementHeight) - 2, 0);
            int lastVisible = Mathf.Min((int)((scrollPosition.y + scrollHeight) / elementHeight) + 2, max);
            return new KeyValuePair<int, int>(firstVisible, lastVisible);
        }
    }
}