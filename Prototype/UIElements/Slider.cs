using System;
using UnityEngine;

namespace Prototype.UIElements
{
    /// Represents a UI slider element that allows users to select a value between a specific range.
    /// The slider allows configuration for minimum, maximum, and default values, along with an optional label and value formatting.
    /// It includes functionality to clamp values to a specified range or allow unclamped values and supports customization of its appearance.
    /// The slider provides multiple constructors for initialization and supports value changes through an event.
    /// Thread safety is not guaranteed for this class.
    public class Slider
    {
        private float _value;
        private float _minValue;
        private float _maxValue;
        private float _defaultValue;
        private string _label;
        private string _format;
        private bool _isDragging = false;
        private bool _isEditingText = false;
        private Vector2 _dragStartPos;
        private float _dragStartValue;
        private string _editText = "";
        private float _clickTime = 0f;
        private Vector2 _clickPos;
        private const float CLICK_THRESHOLD = 0.2f;
        private const float DRAG_THRESHOLD = 3f;

        public float Value
        {
            get => _value;
            set => SetValue(value);
        }

        public float MinValue
        {
            get => _minValue;
            set => _minValue = value;
        }

        public float MaxValue
        {
            get => _maxValue;
            set => _maxValue = value;
        }

        public float DefaultValue
        {
            get => _defaultValue;
            set => _defaultValue = value;
        }

        public string Label
        {
            get => _label;
            set => _label = value;
        }

        public string Format
        {
            get => _format;
            set => _format = value;
        }

        public bool AllowUnclamped;

        public event Action<float> OnValueChanged;

        public Slider(float value, float minValue, float maxValue, string label = "", string format = "F2")
        {
            _value = value;
            _minValue = minValue;
            _maxValue = maxValue;
            _label = label;
            _format = format;
        }

        public Slider(float value, float minValue, float maxValue, float defaultValue, string label = "", string format = "F2")
        {
            _value = value;
            _minValue = minValue;
            _maxValue = maxValue;
            _defaultValue = defaultValue;
            _label = label;
            _format = format;
        }

        public void SetValue(float value, bool allowUnclamped = false)
        {
            float clampedValue = allowUnclamped ? value : Mathf.Clamp(value, _minValue, _maxValue);
            if (Mathf.Abs(_value - clampedValue) > 0.0001f)
            {
                _value = clampedValue;
                OnValueChanged?.Invoke(_value);
            }
        }

        public float Draw()
        {
            return Draw(GUILayout.Height(24));
        }

        public float Draw(params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 24, options);
            return Draw(rect);
        }

        public float Draw(Rect rect)
        {
            Event currentEvent = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (_isEditingText)
            {
                return DrawTextInput(rect, currentEvent, controlID);
            }

            bool isHovering = rect.Contains(currentEvent.mousePosition);
            bool isActive = GUIUtility.hotControl == controlID;

            if (isHovering && currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
            {
                SetValue(_defaultValue);
                currentEvent.Use();
                GUIUtility.hotControl = controlID;
                return _value;
            }

            if (isHovering && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                GUIUtility.hotControl = controlID;
                _clickTime = Time.realtimeSinceStartup;
                _clickPos = currentEvent.mousePosition;
                _dragStartPos = currentEvent.mousePosition;
                _dragStartValue = _value;
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                float dragDistance = Vector2.Distance(currentEvent.mousePosition, _clickPos);

                if (dragDistance > DRAG_THRESHOLD)
                {
                    _isDragging = true;

                    float dragDelta = currentEvent.mousePosition.x - _dragStartPos.x;
                    float sensitivity = (_maxValue - _minValue) / rect.width;

                    float newValue = _dragStartValue + dragDelta * sensitivity;

                    if (currentEvent.shift)
                    {
                        float range = _maxValue - _minValue;
                        float magnitude = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(range)));
                        float snapIncrement = magnitude * 0.1f;
    
                        newValue = Mathf.Round(newValue / snapIncrement) * snapIncrement;
                    }

                    SetValue(newValue);
                    currentEvent.Use();
                }
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                float clickDuration = Time.realtimeSinceStartup - _clickTime;
                float dragDistance = Vector2.Distance(currentEvent.mousePosition, _clickPos);

                if (clickDuration < CLICK_THRESHOLD && dragDistance < DRAG_THRESHOLD && !_isDragging)
                {
                    StartTextEdit();
                }

                _isDragging = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }

            if (isHovering && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag))
            {
                if (GUIUtility.hotControl == 0 || GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = controlID;
                }
            }

            DrawSlider(rect, isHovering, isActive);

            if (rect.Contains(new Vector2(UnityEngine.Input.mousePosition.x, Screen.height - UnityEngine.Input.mousePosition.y)))
            {
                UnityEngine.Input.ResetInputAxes();
            }

            return _value;
        }

        private float DrawTextInput(Rect rect, Event currentEvent, int controlID)
        {
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                CancelTextEdit();
                currentEvent.Use();
                return _value;
            }

            if (currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                ConfirmTextEdit();
                currentEvent.Use();
                return _value;
            }

            if (currentEvent.type == EventType.MouseDown && !rect.Contains(currentEvent.mousePosition))
            {
                ConfirmTextEdit();
                return _value;
            }

            GUIUtility.hotControl = controlID;

            GUI.SetNextControlName("SliderTextInput");
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);

            GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            string newText = GUI.TextField(rect, _editText, textFieldStyle);

            if (newText != _editText)
            {
                _editText = newText;
            }

            GUI.backgroundColor = originalColor;

            if (currentEvent.type == EventType.Repaint)
            {
                GUI.FocusControl("SliderTextInput");
            }

            return _value;
        }

        private void StartTextEdit()
        {
            _isEditingText = true;
            _editText = _value.ToString(_format);
        }

        private void CancelTextEdit()
        {
            _isEditingText = false;
            _editText = "";
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
        }

        private void ConfirmTextEdit()
        {
            if (float.TryParse(_editText, out float newValue))
            {
                SetValue(newValue, AllowUnclamped);
            }

            _isEditingText = false;
            _editText = "";
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
        }

        private void DrawSlider(Rect rect, bool isHovering, bool isActive)
        {
            float clampedValue = Mathf.Clamp(_value, _minValue, _maxValue);
            float normalizedValue = Mathf.InverseLerp(_minValue, _maxValue, clampedValue);

            Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color fillColor = isActive ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.3f, 0.6f, 0.9f, 1f);
            Color borderColor = isHovering ? new Color(0.6f, 0.6f, 0.6f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
            Color textColor = Color.white;

            if (_value < _minValue || _value > _maxValue)
            {
                fillColor = new Color(1f, .7f, 0.3f, 1f);
                borderColor = new Color(1f, .5f, .2f, 1f);
            }

            if (isHovering)
            {
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                fillColor = Color.Lerp(fillColor, Color.white, 0.1f);
            }
            DrawBorder(rect, borderColor, 1f);

            GUI.DrawTexture(rect, Texture.GetOrCreateSolid(backgroundColor));

            if (_value >= _minValue && _value <= _maxValue)
            {
                Rect fillRect = new Rect(rect.x, rect.y, rect.width * normalizedValue, rect.height);
                GUI.DrawTexture(fillRect, Texture.GetOrCreateSolid(fillColor));
            }
            else
            {
                Rect indicatorRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
                GUI.DrawTexture(indicatorRect, Texture.GetOrCreateSolid(new Color(fillColor.r, fillColor.g, fillColor.b, 0.3f)));
            }


            string displayText;
            if (!string.IsNullOrEmpty(_label))
            {
                displayText = $"{_label}: {_value.ToString(_format)}";
            }
            else
            {
                displayText = _value.ToString(_format);
            }

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            
            Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
            GUI.Label(shadowRect, displayText, new GUIStyle(textStyle) { normal = { textColor = Color.black } });
            GUI.Label(rect, displayText, textStyle);
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x-thickness, rect.y-thickness, rect.width+thickness*2, rect.height+thickness*2), Texture.GetOrCreateSolid(color));
        }

        public void ResetToDefault()
        {
            SetValue(_defaultValue);
        }

        public void SetRange(float min, float max)
        {
            _minValue = min;
            _maxValue = max;
        }
    }
}