using System;
using System.Collections.Generic;
using Compositor.KK;
using UnityEngine;

namespace DefaultNamespace
{
    public class CurvePoint
    {
        public Vector2 position;
        public bool selected;

        public CurvePoint(Vector2 pos)
        {
            position = pos;
            selected = false;
        }

        public CurvePoint(float x, float y)
        {
            position = new Vector2(x, y);
            selected = false;
        }
    }

    public class CompositorCurve : IDisposable
    {
        private List<CurvePoint> _points = new List<CurvePoint>();
        private CurvePoint _selectedPoint = null;
        private bool _isDragging = false;
        private AnimationCurve _animationCurve;
        private Texture2D _gridTexture;
        private int _gridSize = 4;

        public event Action<AnimationCurve> OnCurveChanged;

        public AnimationCurve Curve => _animationCurve;
        public List<CurvePoint> Points => _points;

        public CompositorCurve()
        {
            _points.Add(new CurvePoint(0, 0));
            _points.Add(new CurvePoint(1, 1));
            UpdateAnimationCurve();
            GenerateGridTexture();
        }

        public CompositorCurve(AnimationCurve curve)
        {
            if (curve != null && curve.keys.Length > 0)
            {
                foreach (var key in curve.keys)
                {
                    _points.Add(new CurvePoint(key.time, key.value));
                }
            }
            else
            {
                _points.Add(new CurvePoint(0, 0));
                _points.Add(new CurvePoint(1, 1));
            }
            UpdateAnimationCurve();
            GenerateGridTexture();
        }

        public void Draw(Rect rect)
        {
            Event currentEvent = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            GUI.DrawTexture(rect, GUIUtils.GetColorTexture(new Color(0.15f, 0.15f, 0.15f, 1f)));

            DrawGrid(rect);

            DrawCurve(rect);

            DrawPoints(rect, currentEvent, controlID);

            if (rect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2)
            {
                Vector2 localPos = ScreenToLocal(currentEvent.mousePosition, rect);
                AddPoint(localPos);
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                _isDragging = false;
                GUIUtility.hotControl = 0;
            }

            DrawBorder(rect, Color.gray, 1f);
        }

        private void DrawGrid(Rect rect)
        {
            if (_gridTexture != null)
            {
                for (float x = rect.x; x < rect.x + rect.width; x += _gridTexture.width)
                {
                    for (float y = rect.y; y < rect.x + rect.height; y += _gridTexture.height)
                    {
                        float width = Mathf.Min(_gridTexture.width, rect.x + rect.width - x);
                        float height = Mathf.Min(_gridTexture.height, rect.y + rect.height - y);
                        GUI.DrawTextureWithTexCoords(new Rect(x, y, width, height), _gridTexture, new Rect(0, 0, width / _gridTexture.width, height / _gridTexture.height));
                    }
                }
            }

            Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            int majorLines = 5;

            for (int i = 0; i <= majorLines; i++)
            {
                float x = rect.x + (rect.width / majorLines) * i;
                float y = rect.y + (rect.height / majorLines) * i;

                GUI.DrawTexture(new Rect(x, rect.y, 1, rect.height), GUIUtils.GetColorTexture(gridColor));
                GUI.DrawTexture(new Rect(rect.x, y, rect.width, 1), GUIUtils.GetColorTexture(gridColor));
            }
        }

        private void DrawCurve(Rect rect)
        {
            if (_animationCurve == null || _points.Count < 2) return;

            Color curveColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            Vector2 previousPoint = Vector2.zero;

            int segments = Mathf.RoundToInt(rect.width);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float value = _animationCurve.Evaluate(t);
                Vector2 currentPoint = LocalToScreen(new Vector2(t, value), rect);

                if (i > 0)
                {
                    GUIUtils.DrawLine(previousPoint, currentPoint, curveColor);
                }

                previousPoint = currentPoint;
            }
        }

        private void DrawPoints(Rect rect, Event currentEvent, int controlID)
        {
            for (var i = 0; i < _points.Count; i++)
            {
                var point = _points[i];
                Vector2 screenPos = LocalToScreen(point.position, rect);

                float pointSize = point.selected ? 8f : 6f;
                Color pointColor = point.selected ? Color.yellow : Color.white;

                Rect pointRect = new Rect(screenPos.x - pointSize / 2, screenPos.y - pointSize / 2, pointSize, pointSize);
                GUI.DrawTexture(pointRect, GUIUtils.GetColorTexture(pointColor));

                bool isHovering = pointRect.Contains(currentEvent.mousePosition);

                if (isHovering && currentEvent.type == EventType.MouseDown)
                {
                    if (currentEvent.button == 0)
                    {
                        SelectPoint(point);
                        _isDragging = true;
                        GUIUtility.hotControl = controlID;
                        currentEvent.Use();
                    }
                    else if (currentEvent.button == 1 && _points.Count > 2)
                    {
                        RemovePoint(point);
                        currentEvent.Use();
                    }
                }

                if (_isDragging && point.selected && GUIUtility.hotControl == controlID)
                {
                    if (currentEvent.type == EventType.MouseDrag)
                    {
                        Vector2 newPos = ScreenToLocal(currentEvent.mousePosition, rect);

                        if (i == 0)
                            newPos.x = 0f;
                        else if (i == _points.Count - 1)
                            newPos.x = 1;
                        else
                            newPos.x = Mathf.Clamp01(newPos.x);

                        newPos.y = Mathf.Clamp01(newPos.y);
                        point.position = newPos;

                        SortPoints();
                        UpdateAnimationCurve();
                        OnCurveChanged?.Invoke(_animationCurve);
                        currentEvent.Use();
                    }
                }
            }
        }

        private Vector2 LocalToScreen(Vector2 localPos, Rect rect)
        {
            return new Vector2(rect.x + localPos.x * rect.width, rect.y + rect.height - localPos.y * rect.height);
        }

        private Vector2 ScreenToLocal(Vector2 screenPos, Rect rect)
        {
            return new Vector2(Mathf.Clamp01((screenPos.x - rect.x) / rect.width), Mathf.Clamp01(1f - (screenPos.y - rect.y) / rect.height));
        }

        private void SelectPoint(CurvePoint point)
        {
            foreach (var p in _points)
                p.selected = false;

            point.selected = true;
            _selectedPoint = point;
        }

        private void AddPoint(Vector2 position)
        {
            var newPoint = new CurvePoint(position);
            _points.Add(newPoint);
            SortPoints();
            SelectPoint(newPoint);
            UpdateAnimationCurve();
            OnCurveChanged?.Invoke(_animationCurve);
        }

        private void RemovePoint(CurvePoint point)
        {
            if (_points.Count <= 2) return;

            _points.Remove(point);
            if (_selectedPoint == point)
                _selectedPoint = null;

            UpdateAnimationCurve();
            OnCurveChanged?.Invoke(_animationCurve);
        }

        private void SortPoints()
        {
            _points.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        }

        private void UpdateAnimationCurve()
        {
            _animationCurve = new AnimationCurve();

            foreach (var point in _points)
            {
                _animationCurve.AddKey(point.position.x, point.position.y);
            }

            for (var i = 0; i < _animationCurve.length; i++)
            {
                _animationCurve.SmoothTangents(i, 0.3f);
            }
        }

        private void GenerateGridTexture()
        {
            int textureSize = 20;
            _gridTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

            Color darkColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color lightColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            for (var x = 0; x < textureSize; x++)
            {
                for (var y = 0; y < textureSize; y++)
                {
                    bool isGrid = x % _gridSize == 0 || y % _gridSize == 0;
                    _gridTexture.SetPixel(x, y, isGrid ? lightColor : darkColor);
                }
            }

            _gridTexture.Apply();
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), GUIUtils.GetColorTexture(color));
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), GUIUtils.GetColorTexture(color));
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), GUIUtils.GetColorTexture(color));
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), GUIUtils.GetColorTexture(color));
        }

        public void SetCurve(AnimationCurve curve)
        {
            _points.Clear();

            if (curve != null && curve.keys.Length > 0)
            {
                foreach (var key in curve.keys)
                {
                    _points.Add(new CurvePoint(key.time, key.value));
                }
            }
            else
            {
                _points.Add(new CurvePoint(0, 0));
                _points.Add(new CurvePoint(1, 1));
            }
            
            UpdateAnimationCurve();
        }

        public void Dispose()
        {
            if(_gridTexture != null)
                UnityEngine.Object.DestroyImmediate(_gridTexture);
        }
    }
}