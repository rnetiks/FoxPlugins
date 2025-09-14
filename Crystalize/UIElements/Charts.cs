using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crystalize.UIElements
{
    /// <summary>
    /// Utility class for drawing various types of charts in Unity.
    /// </summary>
    public static class Charts
    {
        public class DataPoint
        {
            public string label;
            public float value;
            public Color color;

            public DataPoint(string label, float value, Color color)
            {
                this.label = label;
                this.value = value;
                this.color = color;
            }
        }

        private static Material lineMaterial;
        private static Material GetLineMaterial()
        {
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return lineMaterial;
        }

        public static void DrawLineChart(Rect rect, List<DataPoint> data, Color lineColor, float minValue = 0f, float maxValue = 100f, int smoothness = 0)
        {
            if (data.Count < 2) return;

            GUI.Box(rect, "");

            var points = new Vector2[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                float x = rect.x + (i / (float)(data.Count - 1)) * rect.width;
                float y = rect.y + rect.height - ((data[i].value - minValue) / (maxValue - minValue)) * rect.height;
                points[i] = new Vector2(x, y);
            }

            if (smoothness > 0 && data.Count >= 3)
            {
                // var smoothPoints = Prototype.Curve(points, smoothness);
                // var smoothPoints = Curve.ResamplePoly(Array.ConvertAll(points, input => new Vector3(input.x, input.y, 0)), smoothness);
                // DrawLines(smoothPoints, lineColor, 2f);
            }
            else
            {
                DrawLines(points, lineColor, 2f);
            }
        }

        public static void DrawBarChart(Rect rect, List<DataPoint> data, float maxValue = 100f)
        {
            if (data.Count == 0) return;
            GUI.Box(rect, "");

            float barWidth = rect.width / data.Count * 0.8f;
            float spacing = rect.width / data.Count * 0.2f;

            for (int i = 0; i < data.Count; i++)
            {
                float barHeight = (data[i].value / maxValue) * rect.height;
                var barRect = new Rect(
                    rect.x + i * (barWidth + spacing) + spacing * 0.5f,
                    rect.y + rect.height - barHeight,
                    barWidth,
                    barHeight
                );

                DrawSolidRect(barRect, data[i].color);
            }
        }

        public static void DrawPieChart(Rect rect, List<DataPoint> data)
        {
            if (data.Count == 0) return;

            float total = data.Sum(d => d.value);
            if (total <= 0) return;

            GUI.Box(rect, "");
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.4f;

            float currentAngle = 0f;

            foreach (var dataPoint in data)
            {
                float sliceAngle = (dataPoint.value / total) * 360f;
                DrawPieSlice(center, radius, currentAngle, currentAngle + sliceAngle, dataPoint.color);
                currentAngle += sliceAngle;
            }
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Material mat = GetLineMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            GL.Begin(GL.QUADS);
            GL.Color(color);

            GL.Vertex3(rect.x, rect.y, 0);
            GL.Vertex3(rect.x + rect.width, rect.y, 0);
            GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0);
            GL.Vertex3(rect.x, rect.y + rect.height, 0);

            GL.End();
            GL.PopMatrix();
        }

        private static void DrawLines(Vector2[] points, Color color, float width)
        {
            if (points.Length < 2) return;

            Material mat = GetLineMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawThickLine(points[i], points[i + 1], color, width);
            }

            GL.PopMatrix();
        }

        private static void DrawThickLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (width * 0.5f);

            GL.Begin(GL.QUADS);
            GL.Color(color);

            GL.Vertex3(from.x + perpendicular.x, from.y + perpendicular.y, 0);
            GL.Vertex3(from.x - perpendicular.x, from.y - perpendicular.y, 0);
            GL.Vertex3(to.x - perpendicular.x, to.y - perpendicular.y, 0);
            GL.Vertex3(to.x + perpendicular.x, to.y + perpendicular.y, 0);

            GL.End();
        }

        private static void DrawPieSlice(Vector2 center, float radius, float startAngle, float endAngle, Color color)
        {
            Material mat = GetLineMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);

            float startRad = startAngle * Mathf.Deg2Rad;
            float endRad = endAngle * Mathf.Deg2Rad;

            int segments = Mathf.Max(3, Mathf.RoundToInt((endAngle - startAngle) / 5f));
            float angleStep = (endRad - startRad) / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = startRad + i * angleStep;
                float angle2 = startRad + (i + 1) * angleStep;

                Vector2 point1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                Vector2 point2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

                GL.Vertex3(center.x, center.y, 0);
                GL.Vertex3(point1.x, point1.y, 0);
                GL.Vertex3(point2.x, point2.y, 0);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}