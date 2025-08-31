using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace PoseLib.KKS
{
    public class UITheme
    {
        public GUIStyle WindowStyle { get; private set; }
        public GUIStyle HeaderStyle { get; private set; }
        public GUIStyle TitleStyle { get; private set; }
        public GUIStyle CloseButtonStyle { get; private set; }
        public GUIStyle ControlsPanelStyle { get; private set; }
        public GUIStyle ButtonStyle { get; private set; }
        public GUIStyle LabelStyle { get; private set; }
        public GUIStyle LabelCenterStyle { get; private set; }
        public GUIStyle TextFieldStyle { get; private set; }
        public GUIStyle TextAreaStyle { get; private set; }
        public GUIStyle DropdownStyle { get; private set; }
        public GUIStyle ToggleStyle { get; private set; }
        public GUIStyle WarningStyle { get; private set; }
        public GUIStyle MessageBoxStyle { get; private set; }
        public GUIStyle MessageStyle { get; private set; }
        public GUIStyle PoseItemStyle { get; private set; }
        public GUIStyle PoseNameStyle { get; private set; }
        public GUIStyle LoadButtonStyle { get; private set; }
        public GUIStyle DeleteButtonStyle { get; private set; }
        public GUIStyle PaginationStyle { get; private set; }
        public GUIStyle PaginationButtonStyle { get; private set; }
        public GUIStyle PaginationTextStyle { get; private set; }
        public GUIStyle ModalBackgroundStyle { get; private set; }
        public GUIStyle PreviewBoxStyle { get; private set; }
        public GUIStyle SaveButtonStyle { get; private set; }
        public GUIStyle CancelButtonStyle { get; private set; }
        public bool IsInitialized { get; private set; }


        public void InitializeStyles()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            var darkGray = new Color(0.25f, 0.25f, 0.25f, 0.95f);
            var mediumGray = new Color(0.35f, 0.35f, 0.35f, 0.9f);
            var lightGray = new Color(0.45f, 0.45f, 0.45f, 0.9f);
            var blue = new Color(0.2f, 0.5f, 0.8f, 1f);
            var red = new Color(0.8f, 0.3f, 0.3f, 1f);
            var green = new Color(0.3f, 0.7f, 0.3f, 1f);

            WindowStyle = CreateBoxStyle(darkGray);
            HeaderStyle = CreateBoxStyle(mediumGray);
            ControlsPanelStyle = CreateBoxStyle(new Color(0.3f, 0.3f, 0.3f, 0.8f));
            MessageBoxStyle = CreateBoxStyle(new Color(0.4f, 0.4f, 0.4f, 0.8f));
            PoseItemStyle = CreateBoxStyle(lightGray);
            PaginationStyle = CreateBoxStyle(mediumGray);
            PreviewBoxStyle = CreateBoxStyle(new Color(0.2f, 0.2f, 0.2f, 0.9f));

            TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            CloseButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = CreateColorTexture(red) },
                hover = { textColor = Color.white, background = CreateColorTexture(new Color(0.9f, 0.4f, 0.4f, 1f)) }
            };

            ButtonStyle = CreateButtonStyle(blue, Color.white);
            LoadButtonStyle = CreateButtonStyle(green, Color.white);
            DeleteButtonStyle = CreateButtonStyle(red, Color.white);
            SaveButtonStyle = CreateButtonStyle(green, Color.white);
            CancelButtonStyle = CreateButtonStyle(mediumGray, Color.white);

            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12
            };

            LabelCenterStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            TextFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                normal = { textColor = Color.white, background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)) },
                focused = { textColor = Color.white, background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.9f)) }
            };

            TextAreaStyle = new GUIStyle(GUI.skin.textArea)
            {
                normal = { textColor = Color.white, background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)) },
                focused = { textColor = Color.white, background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.9f)) },
                wordWrap = true
            };

            DropdownStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.white, background = CreateColorTexture(mediumGray) },
                hover = { textColor = Color.white, background = CreateColorTexture(lightGray) },
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            ToggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = Color.white },
                fontSize = 11
            };

            WarningStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(1f, 0.8f, 0.2f, 1f) },
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Italic
            };

            MessageStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            PoseNameStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            PaginationButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.white, background = CreateColorTexture(blue) },
                hover = { textColor = Color.white, background = CreateColorTexture(new Color(0.3f, 0.6f, 0.9f, 1f)) },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            PaginationTextStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            ModalBackgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateColorTexture(new Color(0f, 0f, 0f, 0.5f)) }
            };
        }

        private GUIStyle CreateBoxStyle(Color color)
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateColorTexture(color) }
            };
        }

        private GUIStyle CreateButtonStyle(Color backgroundColor, Color textColor)
        {
            return new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = textColor, background = CreateColorTexture(backgroundColor) },
                hover = { textColor = textColor, background = CreateColorTexture(new Color(backgroundColor.r + 0.1f, backgroundColor.g + 0.1f, backgroundColor.b + 0.1f, backgroundColor.a)) },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }

        private Dictionary<Color, Texture2D> _colorTextures = new Dictionary<Color, Texture2D>();
        private Texture2D CreateColorTexture(Color color)
        {
            if (_colorTextures.TryGetValue(color, out var texture))
                return texture;
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            _colorTextures[color] = texture;
            return texture;
        }
    }
}