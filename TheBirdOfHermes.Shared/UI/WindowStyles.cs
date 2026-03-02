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
        public const float MinWindowWidth = 500f;
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

        private static GUIStyle _windowStyle;
        public static GUIStyle WindowStyle => _windowStyle ?? (_windowStyle = new GUIStyle(GUI.skin.window)
        {
            padding = new RectOffset(2, 2, 18, 2)
        });

        public static string FormatTime(float seconds)
        {
            if (seconds < 0) seconds = 0;
            int min = (int)(seconds / 60f);
            float sec = seconds - min * 60f;
            return min > 0 ? $"{min}:{sec:00.00}" : $"{sec:0.00}s";
        }
    }
}