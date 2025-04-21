using System.Collections.Generic;
using System.Globalization;
using Autumn.Elements;
using PrismaLib.Settings;
using SmartRectV0;
using UnityEngine;

namespace Autumn
{
    public static class PRF
    {
        public static class ExtraColor
        {
            public static readonly Color Empty = new Color(0f, 0f, 0f, 0f);
            public static readonly Color Orange = new Color(1f, 0.35f, 0f, 1f);
        }

        public static string LabelEnabled = "<color=lime>Enabled</color>";
        public static string LabelDisabled = "<color=red>Disabled</color>";

        private static Dictionary<Setting<float>, string> floats = new Dictionary<Setting<float>, string>();
        private static Dictionary<Setting<int>, string> integers = new Dictionary<Setting<int>, string>();

        #region BeginScrollView

        public static Vector2 BeginScrollView(Rect position, Vector2 scrollView, Rect viewRect)
        {
            return GUI.BeginScrollView(position, scrollView, viewRect, Style.ScrollView, Style.ScrollView);
        }

        public static Vector2 BeginScrollView(Rect position, Vector2 scrollView, Rect viewRect, bool alwaysHorizontal,
            bool alwaysVertical)
        {
            return GUI.BeginScrollView(position, scrollView, viewRect, alwaysHorizontal, alwaysVertical,
                Style.ScrollView, Style.ScrollView);
        }

        public static Vector2 BeginScrollView(SmartRect position, Vector2 scrollView, Rect viewRect, bool move = false)
        {
            scrollView = GUI.BeginScrollView(position.ToRect(), scrollView, viewRect, Style.ScrollView,
                Style.ScrollView);
            if (move)
            {
                position.MoveY();
            }

            return scrollView;
        }

        public static Vector2 BeginScrollView(SmartRect position, Vector2 scrollView, Rect viewRect,
            bool alwaysHorizontal, bool alwaysVertical, bool move = false)
        {
            scrollView = GUI.BeginScrollView(position.ToRect(), scrollView, viewRect, alwaysHorizontal, alwaysVertical,
                Style.ScrollView, Style.ScrollView);
            if (move)
            {
                position.MoveY();
            }

            return scrollView;
        }

        #endregion BeginScrollView

        #region Box

        public static void Box(Rect position, string text)
        {
            GUI.Box(position, text, Style.Box);
        }

        public static void Box(SmartRect position, string text, bool move = true)
        {
            GUI.Box(position.ToRect(), text, Style.Box);
            if (move)
            {
                position.MoveY();
            }
        }

        #endregion Box

        #region Button

        public static bool Button(Rect position, string text)
        {
            return GUI.Button(position, text, Style.Button);
        }

        public static bool Button(SmartRect position, string text, bool move = true)
        {
            bool value = GUI.Button(position.ToRect(), text, Style.Button);
            if (move)
            {
                position.MoveY();
            }

            return value;
        }

        #endregion Button

        #region DrawTexture

        public static void DrawTexture(Rect position, Texture tex)
        {
            GUI.DrawTexture(position, tex);
        }

        public static void DrawTexture(SmartRect position, Texture tex, bool move = false)
        {
            GUI.DrawTexture(position.ToRect(), tex);
            if (move)
            {
                position.MoveY();
            }
        }

        #endregion DrawTexture

        #region DropdownMenu

        public static void DropdownMenu(SmartRect rect, Setting<int> selection, string[] selections, bool move = true)
        {
            if (Button(rect, selections[selection.Value], false))
            {
                DropdownSelection.CreateNew(rect.ToRect(), selections, selection);
            }

            if (move)
            {
                rect.MoveY();
            }
        }

        public static void DropdownMenu(GUIBase baseGUI, SmartRect rect, Setting<int> selection, string[] selections,
            bool move = true)
        {
            if (Button(rect, selections[selection.Value], false))
            {
                DropdownSelection.CreateNew(baseGUI, rect.ToRect(), selections, selection);
            }

            if (move)
            {
                rect.MoveY();
            }
        }

        public static void DropdownMenu(SmartRect rect, Setting<int> selection, string[] selections, string label,
            float horizontalOffest, bool move = true)
        {
            Label(rect, label, false);
            rect.MoveOffsetX(horizontalOffest);
            if (Button(rect, selections[selection.Value], false))
            {
                DropdownSelection.CreateNew(rect.ToRect(), selections, selection);
            }

            rect.ResetX();
            if (move)
            {
                rect.MoveY();
            }
        }

        public static void DropdownMenu(GUIBase baseGUI, SmartRect rect, Setting<int> selection, string[] selections,
            string label, float horizontalOffest, bool move = true)
        {
            Label(rect, label, false);
            rect.MoveOffsetX(horizontalOffest);
            if (Button(rect, selections[selection.Value], false))
            {
                DropdownSelection.CreateNew(baseGUI, rect.ToRect(), selections, selection);
            }

            rect.ResetX();
            if (move)
            {
                rect.MoveY();
            }
        }

        #endregion

        #region DropDownMenuScrollable

        public static void DropdownMenuScrollable(SmartRect rect, Setting<int> selection, string[] selections,
            int showItems, bool move = true)
        {
            if (Button(rect, selections[selection.Value], false))
            {
                ScrollableDropdownSelection.CreateNew(rect.ToRect(), selections, selection, showItems);
            }

            if (move)
            {
                rect.MoveY();
            }
        }

        public static void DropdownMenuScrollable(GUIBase baseGUI, SmartRect rect, Setting<int> selection,
            string[] selections, int showItems, bool move = true)
        {
            if (Button(rect, selections[selection.Value], false))
            {
                ScrollableDropdownSelection.CreateNew(baseGUI, rect.ToRect(), selections, selection, showItems);
            }

            if (move)
            {
                rect.MoveY();
            }
        }

        public static void DropdownMenuScrollable(SmartRect rect, Setting<int> selection, string[] selections,
            string label, float horizontalOffest, int showItems, bool move = true)
        {
            Label(rect, label, false);
            rect.MoveOffsetX(horizontalOffest);
            if (Button(rect, selections[selection.Value], false))
            {
                ScrollableDropdownSelection.CreateNew(rect.ToRect(), selections, selection, showItems);
            }

            rect.ResetX();
            if (move)
            {
                rect.MoveY();
            }
        }

        public static void DropdownMenuScrollable(GUIBase baseGUI, SmartRect rect, Setting<int> selection,
            string[] selections, string label, float horizontalOffest, int showItems, bool move = true)
        {
            Label(rect, label, false);
            rect.MoveOffsetX(horizontalOffest);
            if (Button(rect, selections[selection.Value], false))
            {
                ScrollableDropdownSelection.CreateNew(baseGUI, rect.ToRect(), selections, selection, showItems);
            }

            rect.ResetX();
            if (move)
            {
                rect.MoveY();
            }
        }

        #endregion

        public static void EndScrollView()
        {
            GUI.EndScrollView();
        }

        #region HorizontalSlider

        public static float HorizontalSlider(Rect position, float value, string label, float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            return GUI.HorizontalSlider(position, value, 0f, 1f, Style.Slider, Style.SliderBody);
        }

        public static float HorizontalSlider(Rect position, float value, string label, float min, float max,
            float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            return GUI.HorizontalSlider(position, value, min, max, Style.Slider, Style.SliderBody);
        }

        public static void HorizontalSlider(Rect position, Setting<float> value, string label, float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            value.Value = GUI.HorizontalSlider(position, value.Value, 0f, 1f, Style.Slider, Style.SliderBody);
        }

        public static void HorizontalSlider(Rect position, Setting<float> value, string label, float min, float max,
            float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            value.Value = GUI.HorizontalSlider(position, value.Value, min, max, Style.Slider, Style.SliderBody);
        }

        public static float HorizontalSlider(SmartRect position, float value, string label, float offset,
            bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            value = GUI.HorizontalSlider(position.ToRect(), value, 0f, 1f, Style.Slider, Style.SliderBody);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }

            return value;
        }

        public static float HorizontalSlider(SmartRect position, float value, string label, float min, float max,
            float offset, bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            value = GUI.HorizontalSlider(position.ToRect(), value, min, max, Style.Slider, Style.SliderBody);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }

            return value;
        }

        public static void HorizontalSlider(SmartRect position, Setting<float> value, string label, float offset,
            bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            value.Value = GUI.HorizontalSlider(position.ToRect(), value.Value, 0f, 1f, Style.Slider, Style.SliderBody);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }
        }

        public static void HorizontalSlider(SmartRect position, Setting<float> value, string label, float min,
            float max, float offset, bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            value.Value =
                GUI.HorizontalSlider(position.ToRect(), value.Value, min, max, Style.Slider, Style.SliderBody);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }
        }

        #endregion HorizontalSlider

        #region Label

        public static void Label(Rect position, string content)
        {
            GUI.Label(position, content, Style.Label);
        }

        public static void Label(SmartRect position, string content, bool move = false)
        {
            GUI.Label(position.ToRect(), content, Style.Label);
            if (move)
            {
                position.MoveY();
            }
        }

        public static void LabelCenter(Rect position, string content)
        {
            GUI.Label(position, content, Style.LabelCenter);
        }

        public static void LabelCenter(SmartRect position, string content, bool move = false)
        {
            GUI.Label(position.ToRect(), content, Style.LabelCenter);
            if (move)
            {
                position.MoveY();
            }
        }

        #endregion Label

        #region SelectionGrid

        public static int SelectionGrid(Rect position, int value, string[] labels, int xCount)
        {
            int koeff = (int)(labels.Length / xCount);
            position.height = (position.height * koeff) + (Style.VerticalMargin * koeff - Style.VerticalMargin);
            value = GUI.SelectionGrid(position, value, labels, xCount, Style.SelectionGrid);
            return value;
        }

        public static void SelectionGrid(Rect position, Setting<int> value, string[] labels, int xCount)
        {
            int koeff = (int)(labels.Length / xCount);
            position.height = (position.height * koeff) + (Style.VerticalMargin * koeff - Style.VerticalMargin);
            value.Value = GUI.SelectionGrid(position, value.Value, labels, xCount, Style.SelectionGrid);
        }

        public static int SelectionGrid(SmartRect position, int value, string[] labels, int xCount, bool move = false)
        {
            float old = position.Height;
            int koeff = (int)(labels.Length / xCount);
            position.Height = (old * koeff) + (Style.VerticalMargin * koeff - Style.VerticalMargin);
            value = GUI.SelectionGrid(position.ToRect(), value, labels, xCount, Style.SelectionGrid);
            if (move)
            {
                position.MoveY();
            }

            position.Height = old;
            return value;
        }

        public static void SelectionGrid(SmartRect position, Setting<int> value, string[] labels, int xCount,
            bool move = false)
        {
            float old = position.Height;
            int koeff = (int)(labels.Length / xCount);
            position.Height = (old * koeff) + (Style.VerticalMargin * koeff - Style.VerticalMargin);
            value.Value = GUI.SelectionGrid(position.ToRect(), value.Value, labels, xCount, Style.SelectionGrid);
            if (move)
            {
                position.MoveY();
            }

            position.Height = old;
        }

        #endregion SelectionGrid

        #region TextField

        public static string TextField(Rect position, string value, string label, float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            return GUI.TextField(position, value, Style.TextField);
        }

        public static void TextField(Rect position, Setting<string> value, string label, float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            value.Value = GUI.TextField(position, value.Value, Style.TextField);
        }

        public static void TextField(Rect position, Setting<float> val, string label, float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            if (!floats.ContainsKey(val))
            {
                floats.Add(val, val.Value.ToString(CultureInfo.InvariantCulture));
            }

            string text = floats[val];
            text = GUI.TextField(position, text, Style.TextField);
            float.TryParse(text, out val.Value);
            floats[val] = text;
        }

        public static void TextField(Rect position, Setting<int> val, string label, float offset)
        {
            if (offset > 0f)
            {
                GUI.Label(position, label, Style.Label);
                position.x += offset;
                position.width -= offset;
            }

            if (!integers.ContainsKey(val))
            {
                integers.Add(val, val.Value.ToString());
            }

            string text = integers[val];
            text = GUI.TextField(position, text, Style.TextField);
            int.TryParse(text, out val.Value);
            integers[val] = text;
        }

        public static string TextField(SmartRect position, string value, string label, float offset, bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            value = GUI.TextField(position.ToRect(), value, Style.TextField);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }

            return value;
        }

        public static void TextField(SmartRect position, Setting<string> value, string label, float offset,
            bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            value.Value = GUI.TextField(position.ToRect(), value.Value, Style.TextField);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }
        }

        public static void TextField(SmartRect position, Setting<float> val, string label, float offset,
            bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            if (!floats.ContainsKey(val))
            {
                floats.Add(val, val.Value.ToString());
            }

            string text = floats[val];
            text = GUI.TextField(position.ToRect(), text, Style.TextField);
            float.TryParse(text, out val.Value);
            floats[val] = text;
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }
        }

        public static void TextField(SmartRect position, Setting<int> val, string label, float offset,
            bool move = false)
        {
            if (offset > 0f)
            {
                GUI.Label(position.ToRect(), label, Style.Label);
                position.MoveOffsetX(offset);
            }

            if (!integers.ContainsKey(val))
            {
                integers.Add(val, val.Value.ToString());
            }

            string text = integers[val];
            text = GUI.TextField(position.ToRect(), text, Style.TextField);
            int.TryParse(text, out val.Value);
            integers[val] = text;
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }
        }

        #endregion TextField

        #region Toggle

        public static bool Toggle(Rect position, bool val, string label)
        {
            GUI.Label(position, label, Style.Label);
            return GUI.Toggle(
                new Rect(position.x + position.width - Style.Height, position.y, Style.Height, Style.Height), val,
                string.Empty, Style.Toggle);
        }

        public static void Toggle(Rect position, Setting<bool> val, string label)
        {
            GUI.Label(position, label, Style.Label);
            val.Value = GUI.Toggle(
                new Rect(position.x + position.width - Style.Height, position.y, Style.Height, Style.Height), val.Value,
                string.Empty, Style.Toggle);
        }

        public static bool Toggle(SmartRect position, bool val, string label, bool move = false)
        {
            GUI.Label(position.ToRect(), label, Style.Label);
            position.MoveOffsetX(position.Width - Style.Height);
            val = GUI.Toggle(position.ToRect(), val, string.Empty, Style.Toggle);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }

            return val;
        }

        public static void Toggle(SmartRect position, Setting<bool> val, string label, float offset, bool move = false)
        {
            GUI.Label(position.ToRect(), label, Style.Label);
            position.MoveOffsetX(position.Width - Style.Height);
            val.Value = GUI.Toggle(position.ToRect(), val.Value, string.Empty, Style.Toggle);
            position.ResetX();
            if (move)
            {
                position.MoveY();
            }
        }

        #endregion Toggle

        #region ToggleButton

        public static bool ToggleButton(Rect position, bool val, string label)
        {
            GUI.Label(position, label, Style.Label);
            if (GUI.Button(position, val ? LabelEnabled : LabelDisabled, Style.TextButton))
            {
                return !val;
            }

            return val;
        }

        public static void ToggleButton(Rect position, Setting<bool> val, string label)
        {
            GUI.Label(position, label, Style.Label);
            if (GUI.Button(position, val.Value ? LabelEnabled : LabelDisabled, Style.TextButton))
            {
                val.Value = !val.Value;
            }
        }

        public static bool ToggleButton(SmartRect position, bool val, string label, bool move = false)
        {
            GUI.Label(position.ToRect(), label, Style.Label);
            if (GUI.Button(position.ToRect(), val ? LabelEnabled : LabelDisabled, Style.TextButton))
            {
                val = !val;
            }

            if (move)
            {
                position.MoveY();
            }

            return val;
        }

        public static void ToggleButton(SmartRect position, Setting<bool> val, string label, bool move = false)
        {
            GUI.Label(position.ToRect(), label, Style.Label);
            if (GUI.Button(position.ToRect(), val.Value ? LabelEnabled : LabelDisabled, Style.TextButton))
            {
                val.Value = !val.Value;
            }

            if (move)
            {
                position.MoveY();
            }
        }

        #endregion ToggleButton
    }
}