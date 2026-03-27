using System.Collections.Generic;
using UnityEngine;

namespace TheBirdOfHermes.UI
{
    public static class WindowStyles
    {
        public const float HeaderWidth = 150f;
        public const float LaneHeight = 64f;
        public const float RulerHeight = 22f;
        public const float ToolbarHeight = 24f;
        public const float HandleWidth = 6f;
        public const float MinWindowWidth = 600f;
        public const float MinWindowHeight = 200f;

        public static readonly Color WindowBg = new Color(0.12f, 0.12f, 0.14f);
        public static readonly Color ToolbarBg = new Color(0.18f, 0.18f, 0.22f);
        public static readonly Color RulerBg = new Color(0.14f, 0.14f, 0.17f);
        public static readonly Color RulerText = new Color(0.7f, 0.7f, 0.7f);

        public static readonly Color LaneBg = new Color(0.16f, 0.16f, 0.19f);
        public static readonly Color LaneBgAlt = new Color(0.14f, 0.14f, 0.17f);
        public static readonly Color LaneBgSelected = new Color(0.22f, 0.22f, 0.30f);
        public static readonly Color HeaderBg = new Color(0.13f, 0.13f, 0.16f);
        public static readonly Color HeaderBgSelected = new Color(0.18f, 0.18f, 0.26f);
        public static readonly Color LaneSeparator = new Color(0.25f, 0.25f, 0.3f, 0.5f);

        public static readonly Color TrimmedRegion = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        public static readonly Color WaveformBg = new Color(0f, 0f, 0f, 0.3f);

        public static readonly Color SnapLine = new Color(1f, 0.8f, 0.2f, 0.8f);
        public static readonly Color Playhead = Color.white;
        public static readonly Color HandleNormal = new Color(1f, 1f, 1f, 0.4f);
        public static readonly Color HandleHover = new Color(1f, 1f, 1f, 0.8f);
        public static readonly Color HandleDrag = new Color(1f, 0.9f, 0.3f, 1f);
        public static readonly Color DragGhost = new Color(1f, 1f, 1f, 0.15f);
        public static readonly Color SelectionBorder = new Color(0.4f, 0.7f, 1f, 0.9f);
        public static readonly Color HoverBorder = new Color(1f, 1f, 1f, 0.25f);
        public static readonly Color TrimmedWaveform = new Color(1f, 1f, 1f, 0.3f);
        public static readonly Color FadeHandle = new Color(1f, 0.85f, 0.3f, 0.9f);
        public static readonly Color TrackNameBg = new Color(0f, 0f, 0f, 0.6f);

        public static readonly Color[] TrackColors =
        {
            new Color(0.3f, 0.6f, 1.0f),
            new Color(0.3f, 0.85f, 0.5f),
            new Color(1.0f, 0.5f, 0.3f),
            new Color(0.85f, 0.3f, 0.6f),
            new Color(0.6f, 0.4f, 1.0f),
            new Color(1.0f, 0.85f, 0.3f),
            new Color(0.3f, 0.85f, 0.85f),
            new Color(1.0f, 0.4f, 0.4f),
        };

        private static readonly Dictionary<int, Texture2D> TexCache = new Dictionary<int, Texture2D>();

        /// Retrieves a texture filled with the specified color. If a texture with the same color
        /// has already been created and cached, it reuses the cached texture. Otherwise, it generates
        /// a new texture, applies the color, and caches it for future use.
        /// <param name="c">The color to fill the texture.</param>
        /// <returns>A Texture2D filled with the specified color.</returns>
        public static Texture2D GetTexture(Color c)
        {
            int key = c.GetHashCode();
            if (!TexCache.TryGetValue(key, out var tex) || tex == null)
            {
                tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, c);
                tex.Apply();
                tex.hideFlags = HideFlags.DontSave;
                TexCache[key] = tex;
            }
            return tex;
        }

        /// Retrieves a color from the predefined track color palette based on the given index.
        /// The method cycles through the palette if the index exceeds its length.
        /// <param name="index">The index used to retrieve a color from the track color palette.</param>
        /// <returns>A Color from the predefined track color palette corresponding to the given index.</returns>
        public static Color GetTrackColor(int index)
        {
            return TrackColors[index % TrackColors.Length];
        }

        private static GUIStyle _labelBold;
        public static GUIStyle LabelBold => _labelBold ?? (_labelBold = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white },
            padding = new RectOffset(4, 2, 0, 0)
        });

        private static GUIStyle _rulerLabel;
        public static GUIStyle RulerLabel => _rulerLabel ?? (_rulerLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = RulerText },
            padding = new RectOffset(2, 0, 2, 0)
        });

        private static GUIStyle _trackNameLabel;
        public static GUIStyle TrackNameLabel => _trackNameLabel ?? (_trackNameLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white },
            padding = new RectOffset(3, 3, 1, 1)
        });

        private static GUIStyle _menuItemLabel;
        public static GUIStyle MenuItemLabel => _menuItemLabel ?? (_menuItemLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
            padding = new RectOffset(2, 2, 0, 0)
        });

        private static GUIStyle _hintLabel;
        public static GUIStyle HintLabel => _hintLabel ?? (_hintLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.55f, 0.55f, 0.6f) },
            wordWrap = true,
            padding = new RectOffset(2, 2, 0, 0)
        });

        private static GUIStyle _windowStyle;
        public static GUIStyle WindowStyle => _windowStyle ?? (_windowStyle = new GUIStyle(GUI.skin.window)
        {
            padding = new RectOffset(2, 2, 18, 2)
        });

        /// Formats a duration in seconds into a human-readable time string.
        /// If the duration is less than a minute, it is displayed in seconds with two decimal places (e.g., "45.00s").
        /// If the duration is one minute or longer, it is displayed in minutes and seconds (e.g., "2:30.00").
        /// <param name="seconds">The duration in seconds to format. Negative values are treated as zero.</param>
        /// <returns>A string representing the formatted time.</returns>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0) seconds = 0;
            int min = (int)(seconds / 60f);
            float sec = seconds - min * 60f;
            return min > 0 ? $"{min}:{sec:00.00}" : $"{sec:0.00}s";
        }
    }
}