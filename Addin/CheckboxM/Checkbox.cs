using System;
using System.Collections.Generic;
using UnityEngine;

namespace Addin
{
	public class Checkbox
	{
        private bool _state;
        private bool _isHovering;
        private float _clickTime;
        private Vector2 _clickPos;
        private bool _isDragging;
        private float _dragProgress;
        private float _dragStartProgress;
        private float _dragStartMouseX;
        private const float CLICK_THRESHOLD = 0.2f;
        private const float DRAG_THRESHOLD = 5f;
        private float _margin = 2f;
        
        // Singletons
        private static Dictionary<Color, Texture2D> _solidTextures = new Dictionary<Color, Texture2D>();

        private static Color offColor = new Color(0.47f, 0.47f, 0.47f);
        private static Color onColor = new Color(0.4f, 0.7f, 1f, 1f);
        private static Color backgroundColor = bgColorOff;
        private static Color checkColor = new Color(0.47f, 0.47f, 0.47f);
        private static Color borderColorOn = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static Color borderColorOff = new Color(0.4f, 0.4f, 0.4f, 1f);
        private static Color bgColorOn = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static Color bgColorOff = new Color(0.2f, 0.2f, 0.2f, 1f);

        public bool State
        {
            get => _state;
            set => SetState(value);
        }

        public float Margin
        {
            get => _margin;
            set => _margin = value;
        }

        public event Action<bool> OnValueChanged;

        public Checkbox(bool state = false)
        {
            _state = state;
            _dragProgress = state ? 1f : 0f;
        }

        public void SetState(bool state)
        {
            if (_state != state)
            {
                _state = state;
                _dragProgress = state ? 1f : 0f;
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

            if (_isHovering && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                GUIUtility.hotControl = controlID;
                _clickTime = Time.realtimeSinceStartup;
                _clickPos = currentEvent.mousePosition;
                _isDragging = false;
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                ProcessMouseDrag(rect, currentEvent);
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                HandleMouseUp(currentEvent);
            }

            DrawCheckbox(rect);

            if(rect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
            return _state;
        }
        private void ProcessMouseDrag(Rect rect, Event currentEvent)
        {

            float dragDistance = Vector2.Distance(currentEvent.mousePosition, _clickPos);
                
            if (!_isDragging && dragDistance > DRAG_THRESHOLD)
            {
                _isDragging = true;
                _dragStartProgress = _dragProgress;
                _dragStartMouseX = currentEvent.mousePosition.x;
            }

            if (_isDragging)
            {
                float min = Mathf.Min(rect.width, rect.height);
                float maxDragRange = rect.width - min;
                    
                float mouseDelta = currentEvent.mousePosition.x - _dragStartMouseX;
                _dragProgress = Mathf.Clamp01(_dragStartProgress + mouseDelta / maxDragRange);

                bool newState = _dragProgress > 0.5f;
                    
                if (newState != _state)
                {
                    _state = newState;
                    OnValueChanged?.Invoke(_state);
                }
            }
            currentEvent.Use();
        }
        private void HandleMouseUp(Event currentEvent)
        {

            if (!_isDragging)
            {
                float clickDuration = Time.realtimeSinceStartup - _clickTime;
                float dragDistance = Vector2.Distance(currentEvent.mousePosition, _clickPos);

                if (clickDuration < CLICK_THRESHOLD && dragDistance < DRAG_THRESHOLD)
                {
                    SetState(!_state);
                }
            }
            else
            {
                _dragProgress = _state ? 1f : 0f;
            }

            _isDragging = false;
            GUIUtility.hotControl = 0;
            currentEvent.Use();
        }

        private void DrawCheckbox(Rect rect)
        {
            Color borderColor = _isHovering ? borderColorOn : borderColorOff;

            backgroundColor = _isHovering ? bgColorOn : bgColorOff;
            
            DrawBorder(rect, borderColor, 1f);
            GUI.DrawTexture(rect, GetOrCreateSolid(backgroundColor));
            
            float min = Mathf.Min(rect.width, rect.height);
            float maxDragRange = rect.width - min;
            
            float xPos = rect.x + _margin + _dragProgress * maxDragRange;
            Rect innerRect = new Rect(xPos, rect.y + _margin, min - _margin * 2, min - _margin * 2);
            
            checkColor = Color.Lerp(offColor, onColor, _dragProgress);
            
            GUI.DrawTexture(innerRect, GetOrCreateSolid(checkColor));
        }
        
        private static Texture2D GetOrCreateSolid(Color color)
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