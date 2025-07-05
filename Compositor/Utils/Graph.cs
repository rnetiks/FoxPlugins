using System;
using System.Collections.Generic;
using UnityEngine;

namespace Compositor.KK
{
    public class CurveDrawer : IDisposable
    {
        private static Material _lineMaterial;
        private static Material LineMaterial
        {
            get
            {
                if (_lineMaterial == null)
                {
                    _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                    _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                    _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    _lineMaterial.SetInt("_ZWrite", 0);
                }
                return _lineMaterial;
            }
        }

        public enum DrawMode
        {
            Line,
            LineStrip,
            Filled,
            SmoothLine
        }

        public struct CurveStyle
        {
            public Color color;
            public float lineWidth;
            public DrawMode mode;
            public Color fillColor;
            public bool antiAliased;

            public static CurveStyle Default => new CurveStyle
            {
                color = Color.white,
                lineWidth = 1f,
                mode = DrawMode.LineStrip,
                fillColor = new Color(1f, 1f, 1f, 0.3f),
                antiAliased = false
            };
        }

        private List<Vector3> _vertexBuffer = new List<Vector3>();
        private List<Vector2> _tempPoints = new List<Vector2>();

        public CurveDrawer()
        {
        }

        /// <summary>
        /// Draws an AnimationCurve within the specified rect
        /// </summary>
        public void DrawCurve(AnimationCurve curve, Rect rect, CurveStyle style)
        {
            if (curve == null) return;

            int segments = Mathf.Max(2, Mathf.RoundToInt(rect.width * 0.5f));
            _tempPoints.Clear();

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float value = curve.Evaluate(t);
                Vector2 screenPoint = LocalToScreen(new Vector2(t, value), rect);
                _tempPoints.Add(screenPoint);
            }

            DrawPoints(_tempPoints, style);
        }

        /// <summary>
        /// Draws a curve from a list of normalized points (0-1 range)
        /// </summary>
        public void DrawNormalizedCurve(List<Vector2> normalizedPoints, Rect rect, CurveStyle style)
        {
            if (normalizedPoints == null || normalizedPoints.Count < 2) return;

            _tempPoints.Clear();
            foreach (var point in normalizedPoints)
            {
                Vector2 screenPoint = LocalToScreen(point, rect);
                _tempPoints.Add(screenPoint);
            }

            DrawPoints(_tempPoints, style);
        }

        /// <summary>
        /// Draws a curve from screen-space points
        /// </summary>
        public void DrawScreenSpaceCurve(List<Vector2> screenPoints, CurveStyle style)
        {
            if (screenPoints == null || screenPoints.Count < 2) return;
            DrawPoints(screenPoints, style);
        }

        public struct CurveData
        {
            public AnimationCurve curve;
            public Rect rect;
            public CurveStyle style;
        }

        /// <summary>
        /// Draws multiple curves at once (more efficient for multiple curves)
        /// </summary>
        public void DrawMultipleCurves(List<CurveData> curves)
        {
            if (curves == null || curves.Count == 0) return;

            GL.PushMatrix();
            GL.LoadPixelMatrix();

            foreach (var curveData in curves)
            {
                if (curveData.curve == null) continue;

                int segments = Mathf.Max(2, Mathf.RoundToInt(curveData.rect.width * 0.5f));
                _tempPoints.Clear();

                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float value = curveData.curve.Evaluate(t);
                    Vector2 screenPoint = LocalToScreen(new Vector2(t, value), curveData.rect);
                    _tempPoints.Add(screenPoint);
                }

                DrawPointsInternal(_tempPoints, curveData.style, false);
            }

            GL.PopMatrix();
        }

        /// <summary>
        /// Draws a grid background for the curve
        /// </summary>
        public void DrawGrid(Rect rect, int horizontalLines = 5, int verticalLines = 10, Color gridColor = default)
        {
            if (gridColor == default) gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            GL.PushMatrix();
            GL.LoadPixelMatrix();
            LineMaterial.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Color(gridColor);

            for (int i = 0; i <= horizontalLines; i++)
            {
                float y = rect.y + (rect.height * i / horizontalLines);
                GL.Vertex3(rect.x, y, 0);
                GL.Vertex3(rect.x + rect.width, y, 0);
            }

            for (int i = 0; i <= verticalLines; i++)
            {
                float x = rect.x + (rect.width * i / verticalLines);
                GL.Vertex3(x, rect.y, 0);
                GL.Vertex3(x, rect.y + rect.height, 0);
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Draws axis labels and values
        /// </summary>
        public void DrawAxes(Rect rect, Vector2 minValues, Vector2 maxValues, CurveStyle style)
        {
            throw new NotImplementedException();
        }

        private void DrawPoints(List<Vector2> points, CurveStyle style)
        {
            DrawPointsInternal(points, style, true);
        }

        private void DrawPointsInternal(List<Vector2> points, CurveStyle style, bool pushPopMatrix)
        {
            if (points.Count < 2) return;

            if (pushPopMatrix) GL.PushMatrix();
            if (pushPopMatrix) GL.LoadPixelMatrix();
            LineMaterial.SetPass(0);

            switch (style.mode)
            {
                case DrawMode.Line:
                    DrawAsLines(points, style);
                    break;
                case DrawMode.LineStrip:
                    DrawAsLineStrip(points, style);
                    break;
                case DrawMode.Filled:
                    DrawAsFilled(points, style);
                    break;
                case DrawMode.SmoothLine:
                    DrawAsSmooth(points, style);
                    break;
            }

            if (pushPopMatrix) GL.PopMatrix();
        }

        private void DrawAsLines(List<Vector2> points, CurveStyle style)
        {
            GL.Begin(GL.LINES);
            GL.Color(style.color);

            for (int i = 0; i < points.Count - 1; i++)
            {
                GL.Vertex3(points[i].x, points[i].y, 0);
                GL.Vertex3(points[i + 1].x, points[i + 1].y, 0);
            }

            GL.End();
        }

        private void DrawAsLineStrip(List<Vector2> points, CurveStyle style)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(style.color);

            foreach (var point in points)
            {
                GL.Vertex3(point.x, point.y, 0);
            }

            GL.End();
        }

        private void DrawAsFilled(List<Vector2> points, CurveStyle style)
        {
            if (style.fillColor.a > 0)
            {
                GL.Begin(GL.TRIANGLE_STRIP);
                GL.Color(style.fillColor);

                float baseY = points[0].y;
                foreach (var point in points)
                {
                    GL.Vertex3(point.x, baseY, 0);
                    GL.Vertex3(point.x, point.y, 0);
                }

                GL.End();
            }

            DrawAsLineStrip(points, style);
        }

        private void DrawAsSmooth(List<Vector2> points, CurveStyle style)
        {
            if (style.lineWidth <= 1f)
            {
                DrawAsLineStrip(points, style);
                return;
            }

            GL.Begin(GL.TRIANGLES);
            GL.Color(style.color);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];

                Vector2 dir = (p2 - p1).normalized;
                Vector2 perpendicular = new Vector2(-dir.y, dir.x) * style.lineWidth * 0.5f;

                Vector2 v1 = p1 - perpendicular;
                Vector2 v2 = p1 + perpendicular;
                Vector2 v3 = p2 + perpendicular;
                Vector2 v4 = p2 - perpendicular;

                GL.Vertex3(v1.x, v1.y, 0);
                GL.Vertex3(v2.x, v2.y, 0);
                GL.Vertex3(v3.x, v3.y, 0);

                GL.Vertex3(v1.x, v1.y, 0);
                GL.Vertex3(v3.x, v3.y, 0);
                GL.Vertex3(v4.x, v4.y, 0);
            }

            GL.End();
        }

        /// <summary>
        /// Converts normalized coordinates (0-1) to screen space within the given rect
        /// </summary>
        private Vector2 LocalToScreen(Vector2 normalizedPoint, Rect rect)
        {
            return new Vector2(
                rect.x + normalizedPoint.x * rect.width,
                rect.y + rect.height - normalizedPoint.y * rect.height
            );
        }

        /// <summary>
        /// Converts screen coordinates to normalized coordinates (0-1) within the given rect
        /// </summary>
        public Vector2 ScreenToLocal(Vector2 screenPoint, Rect rect)
        {
            return new Vector2(
                (screenPoint.x - rect.x) / rect.width,
                1f - (screenPoint.y - rect.y) / rect.height
            );
        }

        /// <summary>
        /// Gets the curve value at a screen X position
        /// </summary>
        public float GetCurveValueAtScreenX(AnimationCurve curve, float screenX, Rect rect)
        {
            if (curve == null) return 0f;
            
            float normalizedX = (screenX - rect.x) / rect.width;
            normalizedX = Mathf.Clamp01(normalizedX);
            return curve.Evaluate(normalizedX);
        }

        public void Dispose()
        {
            _vertexBuffer?.Clear();
            _tempPoints?.Clear();
        }

        public static void CleanupMaterial()
        {
            if (_lineMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_lineMaterial);
                _lineMaterial = null;
            }
        }
    }

    public static class CurveDrawerExtensions
    {
        public static CurveDrawer.CurveStyle WithColor(this CurveDrawer.CurveStyle style, Color color)
        {
            style.color = color;
            return style;
        }

        public static CurveDrawer.CurveStyle WithLineWidth(this CurveDrawer.CurveStyle style, float width)
        {
            style.lineWidth = width;
            if (width > 1f) style.mode = CurveDrawer.DrawMode.SmoothLine;
            return style;
        }

        public static CurveDrawer.CurveStyle WithFill(this CurveDrawer.CurveStyle style, Color fillColor)
        {
            style.fillColor = fillColor;
            style.mode = CurveDrawer.DrawMode.Filled;
            return style;
        }

        public static CurveDrawer.CurveStyle AsSmooth(this CurveDrawer.CurveStyle style)
        {
            style.mode = CurveDrawer.DrawMode.SmoothLine;
            return style;
        }
    }
}