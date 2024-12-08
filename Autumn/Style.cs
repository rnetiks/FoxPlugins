using System;
using System.Collections.Generic;
using Autumn.Configuration;
using PrismaLib.Settings;
using UnityEngine;

namespace Autumn
{
    public class Style
    {
        private static List<GUIStyle> allStyles;
        private static string[] TextColors;
        private static bool wasLoaded;

        /// <summary>
        /// Style for Box
        /// </summary>
        public static readonly GUIStyle Box = new GUIStyle();

        /// <summary>
        /// Style for Button
        /// </summary>
        public static readonly GUIStyle Button = new GUIStyle();

        /// <summary>
        /// Style for common Label
        /// </summary>
        public static readonly GUIStyle Label = new GUIStyle();

        /// <summary>
        /// Style for centered Label
        /// </summary>
        public static readonly GUIStyle LabelCenter = new GUIStyle();

        /// <summary>
        /// Style for ScrollView
        /// </summary>
        public static readonly GUIStyle ScrollView = GUIStyle.none;

        /// <summary>
        /// Style for SelectionGrid
        /// </summary>
        public static readonly GUIStyle SelectionGrid = new GUIStyle();

        /// <summary>
        /// Style of Slider Line
        /// </summary>
        public static readonly GUIStyle Slider = new GUIStyle();

        /// <summary>
        /// Style of Slider Thumb
        /// </summary>
        public static readonly GUIStyle SliderBody = new GUIStyle();

        /// <summary>
        /// Style for TextButton
        /// </summary>
        /// <remarks>TextButton is analog of Toggle</remarks>
        public static readonly GUIStyle TextButton = new GUIStyle();

        /// <summary>
        /// Style for TextField
        /// </summary>
        public static readonly GUIStyle TextField = new GUIStyle();

        /// <summary>
        /// Style for Toggle
        /// </summary>
        public static readonly GUIStyle Toggle = new GUIStyle();

        public static string[] PublicSettings;
        public static Color[] TextureColors;
        public static Vector3[] TextureDeltas;
        public static bool UseVectors;

        public static string BackgroundHex { get; set; }
        public static int BackgroundTransparency { get; set; } = 250;
        public static float BigLabelOffset { get; private set; }
        public static GUIStyle[] CustomStyles { get; private set; }
        public static string FontName { get; private set; }
        public static int FontSize { get; private set; }
        public static float Height { get; private set; }
        public static int HorizontalMargin { get; private set; }
        public static float LabelOffset { get; private set; }
        public static float LabelOffsetSlider { get; private set; }
        public static string LabelSpace { get; private set; }
        public static float ScreenHeight { get; set; }
        public static float ScreenHeightDefault { get; private set; }
        public static float ScreenWidth { get; set; }
        public static float ScreenWidthDefault { get; private set; }
        public static int VerticalMargin { get; private set; }
        public static float WindowBottomOffset { get; private set; }
        public static float WindowHeight { get; private set; }
        public static float WindowSideOffset { get; private set; }
        public static float WindowTopOffset { get; private set; }
        public static float WindowWidth { get; private set; }

        /// <summary>
        /// Dynamically updates ui size based on current screen resolution
        /// </summary>
        public static void UpdateScaling()
        {
            BigLabelOffset = SetScaling(StyleSettings.BigLabelOffset);
        }

        private static void InitializeStyles()
        {
            // Box
            Box.ApplyStyle(TextAnchor.UpperCenter, FontStyle.Bold, FontSize + 2, true);
            Box.name = "AutumnBox";
            Box.richText = true;

            // Button
            Button.ApplyStyle(TextAnchor.MiddleCenter, FontStyle.Normal, FontSize, true);
            Button.name = "AutumnButton";
            Button.richText = true;

            // Label
            Label.ApplyStyle(TextAnchor.MiddleLeft, FontStyle.Normal, FontSize, true);
            Label.name = "AutumnLabel";
            Label.richText = true;

            LabelCenter.ApplyStyle(TextAnchor.MiddleLeft, FontStyle.Normal, FontSize, true);
            LabelCenter.name = "AutumnLabelC";
            LabelCenter.richText = true;

            // Selection Grid
            SelectionGrid.ApplyStyle(TextAnchor.MiddleCenter, FontStyle.Normal, FontSize, false);
            SelectionGrid.name = "AutumnGrid";
            SelectionGrid.richText = true;

            // Slider
            Slider.name = "AutumnSlider";
            SliderBody.name = "AutumnSliderBody";

            // TextField
            TextField.ApplyStyle(TextAnchor.MiddleLeft, FontStyle.Normal, FontSize, false);
            TextField.name = "AutumnTextField";
            TextField.richText = false;
            TextField.clipping = TextClipping.Clip;

            // Toggle
            Toggle.name = "AutumnToggle";

            // Text Button
            TextButton.ApplyStyle(TextAnchor.MiddleRight, FontStyle.Normal, FontSize, true,
                new[] { Color.white, Colors.orange, Color.yellow, Color.white, Color.white, Color.white });
            TextButton.name = "AutumnTextButton";

            CustomStyles = new[]
            {
                Box, Button, Label, LabelCenter, SelectionGrid, Slider, SliderBody, TextField, Toggle, TextButton
            };
        }

        private static float SetScaling(FloatSetting set)
        {
            if (!UIManager.HUDAutoScaleGUI.Value)
                return set.Value;
            return (float)Math.Round(set.DefaultValue * UIManager.HUDScaleGUI.Value, 0);
        }

        private static int SetScaling(IntSetting set)
        {
            return !UIManager.HUDAutoScaleGUI.Value
                ? set.Value
                : Mathf.RoundToInt(set.DefaultValue * UIManager.HUDScaleGUI.Value);
        }
    }
}