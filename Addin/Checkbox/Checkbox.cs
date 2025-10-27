using System;
using System.Collections.Generic;
using UnityEngine;

namespace Addin
{
    /// Represents a UI checkbox element that allows users to toggle a boolean value.
    /// The checkbox allows configuration for an optional label and supports customization of its appearance.
    /// It includes functionality for click detection and supports value changes through an event.
    /// The checkbox provides multiple constructors for initialization and supports both direct drawing and layout-based drawing.
    /// Thread safety is not guaranteed for this class.
    public class Checkbox
    {
        private bool _state;
        private string _label;
        private float _size = 12f;
        private bool _isHovering = false;
        private float _clickTime = 0f;
        private Vector2 _clickPos;
        private const float CLICK_THRESHOLD = 0.2f;

        public bool State
        {
            get => _state;
            set => SetState(value);
        }

        public string Label
        {
            get => _label;
            set => _label = value;
        }

        public float Size
        {
            get => _size;
            set => _size = Mathf.Max(value, 8f);
        }

        public event Action<bool> OnValueChanged;

        public Checkbox(bool state = false, string label = "")
        {
            _state = state;
            _label = label;
        }

        public void SetState(bool state)
        {
            if (_state != state)
            {
                _state = state;
                OnValueChanged?.Invoke(_state);
            }
        }

        public bool Draw()
        {
            return Draw(GUILayout.Height(24));
        }

        public bool Draw(params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 24, options);
            return Draw(rect);
        }

        public bool Draw(Rect rect)
        {
            Event currentEvent = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            _isHovering = rect.Contains(currentEvent.mousePosition);
            bool isActive = GUIUtility.hotControl == controlID;

            if (_isHovering && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                GUIUtility.hotControl = controlID;
                _clickTime = Time.realtimeSinceStartup;
                _clickPos = currentEvent.mousePosition;
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                float clickDuration = Time.realtimeSinceStartup - _clickTime;
                float dragDistance = Vector2.Distance(currentEvent.mousePosition, _clickPos);

                if (clickDuration < CLICK_THRESHOLD && dragDistance < 5f)
                {
                    SetState(!_state);
                }

                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }

            DrawCheckbox(rect, isActive);

            return _state;
        }

        private void DrawCheckbox(Rect rect, bool isActive)
        {
            Rect checkboxRect = new Rect(rect.x, rect.y, _size, _size);

            Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color borderColor = _isHovering ? new Color(0.6f, 0.6f, 0.6f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
            Color checkColor = isActive ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.3f, 0.6f, 0.9f, 1f);

            if (_isHovering)
            {
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                checkColor = Color.Lerp(checkColor, Color.white, 0.1f);
            }

            DrawBorder(checkboxRect, borderColor, 1f);
            GUI.DrawTexture(checkboxRect, GetOrCreateSolid(backgroundColor));

            if (_state)
            {
                Rect innerRect = new Rect(checkboxRect.x + 2, checkboxRect.y + 2, checkboxRect.width - 4, checkboxRect.height - 4);
                GUI.DrawTexture(innerRect, GetOrCreateSolid(checkColor));
            }

            if (!string.IsNullOrEmpty(_label))
            {
                Rect labelRect = new Rect(checkboxRect.xMax + 8, checkboxRect.y + (_size - rect.height) * 0.5f, rect.width - _size - 8, rect.height);
                
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white },
                    fontSize = 11,
                    fontStyle = FontStyle.Normal
                };

                GUI.Label(labelRect, _label, labelStyle);
            }
        }
        
        private Dictionary<Color, Texture2D> _solidTextures = new Dictionary<Color, Texture2D>();
        private Texture2D GetOrCreateSolid(Color color)
        {
            if (!_solidTextures.TryGetValue(color, out Texture2D texture))
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, color);
                texture.Apply();
                _solidTextures.Add(color, texture);
            }
            return texture;
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x - thickness, rect.y - thickness, rect.width + thickness * 2, rect.height + thickness * 2), GetOrCreateSolid(color));
        }

        public void Toggle()
        {
            SetState(!_state);
        }
    }
}