using System;
using System.Collections.Generic;
using System.Linq;
using Compositor.KK;
using UnityEngine;

namespace DefaultNamespace
{
    public class GradientStop
    {
        public float position;
        public Color color;
        public bool selected;

        public GradientStop(float pos, Color col)
        {
            position = pos;
            color = col;
            selected = false;
        }
    }

    public class CompositorGradient : IDisposable
    {
        private List<GradientStop> _stops = new List<GradientStop>();
        private GradientStop _selectedStop = null;
        private bool _isDragging = false;
        private Texture2D _gradientTexture;
        private CompositorColorSelector _colorSelector;
        private bool _showColorSelector = false;
        private int _textureWidth = 256;

        public event Action<List<GradientStop>> OnGradientChanged;

        public List<GradientStop> Stops => _stops;

        public CompositorGradient()
        {
            _stops.Add(new GradientStop(0f, Color.black));
            _stops.Add(new GradientStop(1f, Color.white));
            _colorSelector = new CompositorColorSelector();
            _colorSelector.OnColorChanged += OnColorSelectorChanged;
            UpdateGradientTexture();
        }

        public CompositorGradient(List<GradientStop> stops)
        {
            _stops = stops ?? new List<GradientStop>();
            if (_stops.Count == 0)
            {
                _stops.Add(new GradientStop(0f, Color.black));
                _stops.Add(new GradientStop(1f, Color.white));
            }
            _colorSelector = new CompositorColorSelector();
            _colorSelector.OnColorChanged += OnColorSelectorChanged;
            UpdateGradientTexture();
        }

        public void Draw(Rect rect)
        {
            float barHeight = 30f;
            float stopHeight = 15f;
            float colorSelectorHeight = 150f;

            Rect gradientRect = new Rect(rect.x, rect.y, rect.width, barHeight);
            Rect stopsRect = new Rect(rect.x, rect.y + barHeight, rect.width, stopHeight);

            DrawGradientBar(gradientRect);
            DrawGradientStops(stopsRect, gradientRect);

            if (_showColorSelector && _selectedStop != null)
            {
                Rect colorRect = new Rect(rect.x, rect.y + barHeight + stopHeight + 5, rect.width, colorSelectorHeight);
                DrawColorSelector(colorRect);
            }
        }

        private void DrawGradientBar(Rect rect)
        {
            Event currentEvent = Event.current;

            if (_gradientTexture != null)
            {
                GUI.DrawTexture(rect, _gradientTexture);
            }

            DrawBorder(rect, Color.gray, 1f);

            if (rect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2)
            {
                float position = Mathf.Clamp01((currentEvent.mousePosition.x - rect.x) / rect.width);
                Color color = EvaluateGradient(position);
                AddStop(position, color);
                currentEvent.Use();
            }
        }
        
        private void AddStop(float position, Color color)
        {
            var newStop = new GradientStop(position, color);
            _stops.Add(newStop);
            SortStops();
            SelectStop(newStop);
            UpdateGradientTexture();
            OnGradientChanged?.Invoke(_stops);
        }

        private void DrawGradientStops(Rect stopsRect, Rect gradientRect)
        {
            Event currentEvent = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            for (int i = 0; i < _stops.Count; i++)
            {
                var stop = _stops[i];
                DrawGradientStop(stop, stopsRect, gradientRect, i, currentEvent, controlID);
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                _isDragging = false;
                GUIUtility.hotControl = 0;
            }
        }

        private void DrawGradientStop(GradientStop stop, Rect stopsRect, Rect gradientRect, int index, Event currentEvent, int controlID)
        {
            float x = gradientRect.x + stop.position * gradientRect.width;
            Rect stopRect = new Rect(x - 6, stopsRect.y, 12, stopsRect.height);

            Color handleColor = stop.selected ? Color.yellow : Color.white;
            GUI.DrawTexture(stopRect, GUIUtils.GetColorTexture(handleColor));

            Rect colorRect = new Rect(x - 4, stopsRect.y + 2, 8, stopsRect.height - 4);
            GUI.DrawTexture(colorRect, GUIUtils.GetColorTexture(stop.color));

            bool isHovering = stopRect.Contains(currentEvent.mousePosition);

            if (isHovering && currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 0)
                {
                    SelectStop(stop);
                    _isDragging = true;
                    GUIUtility.hotControl = controlID;
                    currentEvent.Use();
                }
                else if (currentEvent.button == 1 && _stops.Count > 2)
                {
                    RemoveStop(stop);
                    currentEvent.Use();
                }
            }

            if (_isDragging && stop.selected && GUIUtility.hotControl == controlID)
            {
                if (currentEvent.type == EventType.MouseDrag)
                {
                    float newPosition = Mathf.Clamp01((currentEvent.mousePosition.x - gradientRect.x) / gradientRect.width);
                    stop.position = newPosition;
                    SortStops();
                    UpdateGradientTexture();
                    OnGradientChanged?.Invoke(_stops);
                    currentEvent.Use();
                }
            }
        }

        private void DrawColorSelector(Rect rect)
        {
            GUI.Box(rect, "");

            Rect innerRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 35);
            _colorSelector.Draw(innerRect);

            Rect closeRect = new Rect(rect.x + rect.width - 60, rect.y + rect.height, 55, 20);
            if (GUI.Button(closeRect, "Close"))
            {
                _showColorSelector = false;
                GUIUtility.hotControl = 0;
            }
        }

        private void OnColorSelectorChanged(Color newColor)
        {
            if (_selectedStop != null)
            {
                _selectedStop.color = newColor;
                UpdateGradientTexture();
                OnGradientChanged?.Invoke(_stops);
            }
        }


        private void SelectStop(GradientStop stop)
        {
            foreach (var s in _stops)
                s.selected = false;

            stop.selected = true;
            _selectedStop = stop;
            _showColorSelector = true;
            _colorSelector.SelectedColor = stop.color;
        }

        private void RemoveStop(GradientStop stop)
        {
            if (_stops.Count <= 2) return;

            _stops.Remove(stop);
            if (_selectedStop == stop)
            {
                _selectedStop = null;
                _showColorSelector = false;
            }
            UpdateGradientTexture();
            OnGradientChanged?.Invoke(_stops);
        }

        private void SortStops()
        {
            _stops.Sort((a, b) => a.position.CompareTo(b.position));
        }

        private Color EvaluateGradient(float position)
        {
            position = Mathf.Clamp01(position);

            if (_stops.Count == 0) return Color.white;
            if (_stops.Count == 1) return _stops[0].color;

            GradientStop leftStop = _stops[0];
            GradientStop rightStop = _stops[_stops.Count - 1];

            for (int i = 0; i < _stops.Count - 1; i++)
            {
                if (position >= _stops[i].position && position <= _stops[i + 1].position)
                {
                    leftStop = _stops[i];
                    rightStop = _stops[i + 1];
                    break;
                }
            }

            if (Mathf.Approximately(leftStop.position, rightStop.position))
                return leftStop.color;

            float t = Mathf.InverseLerp(leftStop.position, rightStop.position, position);
            return Color.Lerp(leftStop.color, rightStop.color, t);
        }


        private void UpdateGradientTexture()
        {
            if (_gradientTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_gradientTexture);
            }

            _gradientTexture = new Texture2D(_textureWidth, 1, TextureFormat.RGB24, false);

            for (int x = 0; x < _textureWidth; x++)
            {
                float position = (float)x / (_textureWidth - 1);
                Color color = EvaluateGradient(position);
                _gradientTexture.SetPixel(x, 0, color);
            }

            _gradientTexture.Apply();
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), GUIUtils.GetColorTexture(color));
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), GUIUtils.GetColorTexture(color));
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), GUIUtils.GetColorTexture(color));
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), GUIUtils.GetColorTexture(color));
        }

        public Gradient ToUnityGradient()
        {
            Gradient gradient = new Gradient();

            var colorKeys = _stops.Select(s => new GradientColorKey(s.color, s.position)).ToArray();
            var alphaKeys = _stops.Select(s => new GradientAlphaKey(s.color.a, s.position)).ToArray();

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        public void Dispose()
        {
            if (_gradientTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_gradientTexture);
            }
            _colorSelector?.Dispose();
        }
    }
}