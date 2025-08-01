using System;
using System.Diagnostics;
using Compositor.KK;
using Compositor.KKS.Utils;
using UnityEngine;

namespace DefaultNamespace
{
    public class CompositorColorSelector
    {
        private Color _selectedColor = Color.white;
        private float _hue = 0f;
        private float _saturation = 1f;
        private float _brightness = 1f;
        private Texture2D _colorWheelTexture;
        private ColorWheelShape _shape = ColorWheelShape.Circle;
        private int _textureSize = 128;
        private bool _isDirty = true;

        public enum ColorWheelShape
        {
            Circle,
            Rectangle,
            // Triangle
        }

        public Color SelectedColor
        {
            get => _selectedColor;
            set => SetColor(value);
        }

        public ColorWheelShape Shape
        {
            get => _shape;
            set
            {
                if (_shape != value)
                {
                    _shape = value;
                    _isDirty = true;
                }
            }
        }

        public event Action<Color> OnColorChanged;

        public CompositorColorSelector()
        {
        }

        public CompositorColorSelector(Color initialColor)
        {
            SetColor(initialColor);
        }

        public void SetColor(Color color)
        {
            _selectedColor = color;
            Color.RGBToHSV(color, out _hue, out _saturation, out _brightness);
            _isDirty = true;
            OnColorChanged?.Invoke(_selectedColor);
        }

        public Color Draw(Rect rect)
        {
            if (_isDirty)
            {
                GenerateColorWheel();
                _isDirty = false;
            }

            float sliderWidth = 20f;
            float spacing = 5f;
            Rect wheelRect = new Rect(rect.x, rect.y, rect.width - sliderWidth - spacing, rect.height);
            Rect sliderRect = new Rect(rect.x + wheelRect.width + spacing, rect.y, sliderWidth, rect.height);

            DrawColorWheel(wheelRect);

            DrawBrightnessSlider(sliderRect);

            DrawShapeSelector(new Rect(rect.x, rect.y + rect.height + 2, rect.width, 20));

            return _selectedColor;
        }

        private void DrawColorWheel(Rect rect)
        {
            Event currentEvent = Event.current;

            rect = new Rect(rect.x, rect.y, Mathf.Min(rect.width, rect.height), Mathf.Min(rect.width, rect.height));

            if (_colorWheelTexture != null)
            {
                GUI.DrawTexture(rect, _colorWheelTexture);
            }
            

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            bool isHovering = rect.Contains(currentEvent.mousePosition);

            if (isHovering && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag))
            {
                if (currentEvent.button == 0)
                {
                    GUIUtility.hotControl = controlID;

                    Vector2 localPos = currentEvent.mousePosition - new Vector2(rect.x, rect.y);
                    Vector2 normalizedPos = new Vector2(localPos.x / rect.width, localPos.y / rect.height);

                    if (IsValidPosition(normalizedPos))
                    {
                        var h = PositionToHS(normalizedPos);
                        _hue = h.h;
                        _saturation = h.s;
                        UpdateColor();
                    }

                    currentEvent.Use();
                }
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                currentEvent.Use();
                Entry.Logger.LogDebug("Mouse up?");
            }

            DrawSelectionIndicator(rect);
        }

        private void DrawBrightnessSlider(Rect rect)
        {
            Event currentEvent = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            Texture2D brightnessTexture = CreateBrightnessTexture(rect);
            GUI.DrawTexture(rect, brightnessTexture);

            bool isHovering = rect.Contains(currentEvent.mousePosition);

            if (isHovering && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag))
            {
                if (currentEvent.button == 0)
                {
                    GUIUtility.hotControl = controlID;

                    float localY = currentEvent.mousePosition.y - rect.y;
                    _brightness = Mathf.Clamp01(1f - (localY / rect.height));
                    UpdateColor();
                    _isDirty = true;

                    currentEvent.Use();
                }
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
            }

            float indicatorY = rect.y + rect.height * (1f - _brightness);
            GUI.DrawTexture(new Rect(rect.x - 2, indicatorY - 1, rect.width + 4, 2), GUIUtils.GetColorTexture(Color.white));

            UnityEngine.Object.DestroyImmediate(brightnessTexture);
        }

        private void DrawShapeSelector(Rect rect)
        {
            var shapes = Enum.GetValues(typeof(ColorWheelShape));
            float buttonWidth = rect.width / shapes.Length;

            for (int i = 0; i < shapes.Length; i++)
            {
                Rect buttonRect = new Rect(rect.x + i * buttonWidth, rect.y, buttonWidth - 2, rect.height);
                ColorWheelShape shape = (ColorWheelShape)shapes.GetValue(i);

                bool isSelected = _shape == shape;
                var buttonStyle = isSelected ? Compositor.CompositorStyles.FilterButtonSelected : Compositor.CompositorStyles.FilterButton;

                if (GUI.Button(buttonRect, shape.ToString(), buttonStyle))
                {
                    Shape = shape;
                    GUIUtility.hotControl = 0;
                }
            }
        }

        private void GenerateColorWheel()
        {
            Entry.Logger.LogDebug($"Generating ColorWheel with shape {_shape} and texture size {_textureSize}");
            var startNew = Stopwatch.StartNew();
            if (_colorWheelTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_colorWheelTexture);
            }

            _colorWheelTexture = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);

            for (int x = 0; x < _textureSize; x++)
            {
                for (int y = 0; y < _textureSize; y++)
                {
                    Vector2 normalizedPos = new Vector2((float)x / _textureSize, (float)y / _textureSize);

                    Color pixelColor = Color.clear;

                    if (IsValidPosition(normalizedPos))
                    {
                        normalizedPos.y = 1f - normalizedPos.y;
                        var h = PositionToHS(normalizedPos);
                        pixelColor = Color.HSVToRGB(h.h, h.s, _brightness);
                    }

                    _colorWheelTexture.SetPixel(x, y, pixelColor);
                }
            }

            _colorWheelTexture.Apply();
            Entry.Logger.LogDebug($"Finished generating ColorWheel. Took {startNew.ElapsedMilliseconds}ms");
        }

        private bool IsValidPosition(Vector2 normalizedPos)
        {
            switch (_shape)
            {
                case ColorWheelShape.Circle:
                    Vector2 center = Vector2.one * 0.5f;
                    return Vector2.Distance(normalizedPos, center) <= 0.5f;

                // case ColorWheelShape.Triangle:
                //     return IsInTriangle(normalizedPos);

                case ColorWheelShape.Rectangle:
                default:
                    return true;
            }
        }

        private bool IsInTriangle(Vector2 pos)
        {
            Vector2 v1 = new Vector2(0.5f, 0.1f);
            Vector2 v2 = new Vector2(0.1f, 0.9f);
            Vector2 v3 = new Vector2(0.9f, 0.9f);

            return Vector.IsPointInTriangle(pos, v1, v2, v3);
        }

        public class HSStruct
        {
            public float h, s;
            public HSStruct(float h, float s)
            {
                this.h = h;
                this.s = s;
            }
        }

        private HSStruct PositionToHS(Vector2 normalizedPos)
        {
            switch (_shape)
            {
                case ColorWheelShape.Circle:
                    Vector2 center = Vector2.one * 0.5f;
                    Vector2 dir = normalizedPos - center;
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    float hue = (angle + Mathf.PI) / (2 * Mathf.PI);
                    float saturation = Mathf.Clamp01(dir.magnitude / 0.5f);
                    return new HSStruct(hue, saturation);

                case ColorWheelShape.Rectangle:
                    return new HSStruct(normalizedPos.x, normalizedPos.y);

                // case ColorWheelShape.Triangle:
                //     // TODO
                //     return new HSStruct(normalizedPos.x, normalizedPos.y);

                default:
                    return new HSStruct(normalizedPos.x, normalizedPos.y);
            }
        }

        private void DrawSelectionIndicator(Rect rect)
        {
            Vector2 indicatorPos = HSToPosition(_hue, _saturation);
            Vector2 screenPos = new Vector2(
                rect.x + indicatorPos.x * rect.width,
                rect.y + indicatorPos.y * rect.height
            );

            Color indicatorColor = _brightness > 0.5f ? Color.black : Color.white;
            GUI.DrawTexture(new Rect(screenPos.x - 6, screenPos.y - 1, 12, 2), GUIUtils.GetColorTexture(indicatorColor));
            GUI.DrawTexture(new Rect(screenPos.x - 1, screenPos.y - 6, 2, 12), GUIUtils.GetColorTexture(indicatorColor));
        }

        /// Converts hue and saturation values into a position on the color wheel or rectangle.
        /// The conversion is based on the currently selected color wheel shape.
        /// <param name="h">The hue value, typically between 0 and 1.</param>
        /// <param name="s">The saturation value, typically between 0 and 1.</param>
        /// <return>A Vector2 representing the position on the color wheel or rectangle based on the input values.</return>
        private Vector2 HSToPosition(float h, float s)
        {
            Vector2 pos;
            switch (_shape)
            {
                case ColorWheelShape.Circle:
                    float angle = h * 2 * Mathf.PI - Mathf.PI;
                    float radius = s * 0.5f;
                    Vector2 center = Vector2.one * 0.5f;
                    return center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                case ColorWheelShape.Rectangle:
                    return new Vector2(h, s);

                // case ColorWheelShape.Triangle:
                //     return new Vector2(h, s); // TODO

                default:
                    return new Vector2(h, s);
            }
        }

        private Texture2D CreateBrightnessTexture(Rect rect)
        {
            int height = Mathf.Max(1, (int)rect.height);
            Texture2D texture = new Texture2D(1, height, TextureFormat.RGB24, false);

            Color baseColor = Color.HSVToRGB(_hue, _saturation, 1f);

            for (int y = 0; y < height; y++)
            {
                float brightness = (float)y / height;
                Color color = Color.HSVToRGB(_hue, _saturation, brightness);
                texture.SetPixel(0, y, color);
            }

            texture.Apply();
            return texture;
        }

        private void UpdateColor()
        {
            _selectedColor = Color.HSVToRGB(_hue, _saturation, _brightness);
            OnColorChanged?.Invoke(_selectedColor);
        }

        public void Dispose()
        {
            if (_colorWheelTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_colorWheelTexture);
            }
        }
    }
}