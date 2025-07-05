using System.Collections.Generic;
using UnityEngine;

namespace Compositor.KK
{
    public class GUIUtils
    {
        private static Dictionary<int, Texture2D> _colorTextures = new Dictionary<int, Texture2D>();
        private static GUIStyle _centeredLabelStyle;

        public static GUIStyle CenteredLabelStyle
        {
            get
            {
                if (_centeredLabelStyle == null)
                {
                    _centeredLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return _centeredLabelStyle;
            }
        }

        public static Texture2D GetColorTexture(Color color)
        {
            int colorKey = color.GetHashCode();

            if (!_colorTextures.TryGetValue(colorKey, out Texture2D texture))
            {
                texture = CreateColorTexture(color);
                _colorTextures[colorKey] = texture;
            }

            return texture;
        }

        private static Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static void DrawLine(Vector2 start, Vector2 end, Color color, float width = 2f)
        {
            Vector2 dir = end - start;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Rect lineRect = new Rect(start.x, start.y - width / 2, distance, width);

            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);

            Color savedColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(lineRect, GetColorTexture(Color.white));
            GUI.color = savedColor;

            GUI.matrix = savedMatrix;
        }

        /// <summary>
        /// Draws a Bézier curve between two points with specified properties, such as color, width, and segment count.
        /// </summary>
        /// <param name="start">The starting point of the Bezier curve.</param>
        /// <param name="end">The ending point of the Bezier curve.</param>
        /// <param name="color">The color of the Bezier curve.</param>
        /// <param name="width">The width of the curve's line. Default is 2f.</param>
        /// <param name="segments">The number of interpolated segments used to draw the curve. Default is 20.</param>
        public static void DrawBezierCurve(Vector2 start, Vector2 end, Color color, float width = 2f, int segments = 20, float midpoint = 100)
        {
            Vector2 startTangent = start + Vector2.right * Mathf.Abs(midpoint);
            Vector2 endTangent = end + Vector2.left * Mathf.Abs(midpoint);
            for (var i = 0; i < segments; i++)
            {
                float t1 = (float)i / segments;
                float t2 = (float)(i + 1) / segments;

                Vector2 p1 = CalculateBezierPoint(t1, start, startTangent, endTangent, end);
                Vector2 p2 = CalculateBezierPoint(t2, start, startTangent, endTangent, end);

                DrawLine(p1, p2, color, width);
            }
        }

        private static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            float uuu = uu * u;
            float ttt = tt * t;
            Vector2 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;
            return p;
        }

        /// <summary>
        /// Scales a rectangular area by a specified multiplier, applying an additional offset to its position.
        /// </summary>
        /// <param name="rect">The original rectangle to be scaled.</param>
        /// <param name="scale">The scale factor to be applied to the rectangle.</param>
        /// <param name="offset">The vector by which to offset the rectangle's position after scaling.</param>
        /// <returns>A new rectangle that has been scaled and offset based on the provided parameters.</returns>
        public static Rect ScaleRect(Rect rect, float scale, Vector2 offset)
        {
            return new Rect((rect.x + offset.x) * scale, (rect.y + offset.y) * scale, rect.width * scale, rect.height * scale);
        }

        /// <summary>
        /// Scales a 2D vector by a specified scale factor and applies an offset to the result.
        /// </summary>
        /// <param name="vector2">The input 2D vector to be scaled and offset.</param>
        /// <param name="scale">The factor by which to scale the vector.</param>
        /// <param name="offset">The offset to apply to the vector after scaling.</param>
        /// <returns>A new scaled and offset 2D vector.</returns>
        public static Vector2 ScaleVector2(Vector2 vector2, float scale, Vector2 offset)
        {
            return (vector2 + offset) * scale;
        }

        public static class Colors
        {
            public static readonly Color Background = new Color(0.15f, 0.15f, 0.15f, 1f);
            public static readonly Color BackgroundGrid = new Color(0.18f, 0.18f, 0.18f, 1f);
            public static readonly Color Header = new Color(0.12f, 0.12f, 0.12f, 1f);
            public static readonly Color HeaderAccent = new Color(0.2f, 0.25f, 0.3f, 1f);
            public static readonly Color NodeBackground = new Color(0.25f, 0.25f, 0.25f, 0.95f);
            public static readonly Color NodeBackgroundHover = new Color(0.3f, 0.3f, 0.3f, 0.95f);
            public static readonly Color NodeHeader = new Color(0.2f, 0.2f, 0.2f, 1f);
            public static readonly Color NodeBorder = new Color(0.4f, 0.4f, 0.4f, 1f);
            public static readonly Color NodeSelected = new Color(0.3f, 0.5f, 0.8f, 1f);
            public static readonly Color NodeSelectedBorder = new Color(0.4f, 0.7f, 1f, 1f);
            public static readonly Color ButtonPrimary = new Color(0.2f, 0.4f, 0.6f, 1f);
            public static readonly Color ButtonPrimaryHover = new Color(0.25f, 0.5f, 0.75f, 1f);
            public static readonly Color ButtonSecondary = new Color(0.35f, 0.35f, 0.35f, 1f);
            public static readonly Color ButtonSecondaryHover = new Color(0.45f, 0.45f, 0.45f, 1f);
            public static readonly Color ButtonSuccess = new Color(0.2f, 0.6f, 0.3f, 1f);
            public static readonly Color ButtonDanger = new Color(0.6f, 0.2f, 0.2f, 1f);
            public static readonly Color Connection = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            public static readonly Color ConnectionHighlight = new Color(1f, 0.7f, 0.3f, 1f);
            public static readonly Color NodeInput = new Color(0.3f, 0.8f, 0.9f, 1f);
            public static readonly Color NodeOutput = new Color(0.9f, 0.6f, 0.3f, 1f);
            public static readonly Color PortConnected = new Color(0.2f, 0.8f, 0.4f, 1f);
            public static readonly Color TextPrimary = new Color(1, 1, 1, 1f);
            public static readonly Color TextSecondary = new Color(0.7f, 0.7f, 0.7f, 1f);
            public static readonly Color TextAccent = new Color(0.6f, 0.8f, 1f, 1f);
            public static readonly Color TextSuccess = new Color(0.4f, 0.8f, 0.5f, 1f);
            public static readonly Color TextWarning = new Color(0.9f, 0.7f, 0.3f, 1f);

            public static class SocketColors
            {
                public static readonly Color RGBA = new Color(1f, 0.8f, 0.2f, 1f);
                public static readonly Color Alpha = new Color(0.7f, 0.7f, 0.7f, 1f);
                public static readonly Color Vector = new Color(0.4f, 0.6f, 1f, 1f);

                public static Color GetSocketColor(SocketType socketType)
                {
                    switch (socketType)
                    {
                        case SocketType.RGBA:
                            return RGBA;
                        case SocketType.Alpha:
                            return Alpha;
                        case SocketType.Vector:
                            return Vector;
                        default:
                            return Color.white;
                    }
                }
            }
        }
    }
}