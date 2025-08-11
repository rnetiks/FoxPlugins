using UnityEngine;

namespace Prototype.UIElements
{
    /// <summary>
    /// Provides static utility methods for rendering interactive color pickers in various shapes (rectangle, triangle, wheel, etc.)
    /// and retrieving selected colors based on user input.
    /// </summary>
    public static class ColorPicker
    {
        // Material for GL rendering - create once and reuse
        private static Material colorMaterial;
        private static Material GetColorMaterial()
        {
            if (colorMaterial == null)
            {
                // Use Unity's built-in shader for UI elements with vertex colors
                colorMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                colorMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return colorMaterial;
        }

        /// <summary>
        /// Draws a rectangle color picker with hue on X-axis and saturation on Y-axis
        /// </summary>
        /// <param name="rect">Rectangle to draw in</param>
        /// <param name="value">HSV Value component (brightness) 0-1</param>
        /// <returns>Color at mouse position if clicked, otherwise Color.clear</returns>
        public static Color DrawColorRectangle(Rect rect, float value = 1f)
        {
            Material mat = GetColorMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            int hueSteps = 32; // Number of hue divisions
            int satSteps = 16; // Number of saturation divisions

            float stepWidth = rect.width / hueSteps;
            float stepHeight = rect.height / satSteps;

            GL.Begin(GL.QUADS);

            for (int h = 0; h < hueSteps; h++)
            {
                for (int s = 0; s < satSteps; s++)
                {
                    float hue1 = (h / (float)hueSteps) * 360f;
                    float hue2 = ((h + 1) / (float)hueSteps) * 360f;
                    float sat1 = s / (float)satSteps;
                    float sat2 = (s + 1) / (float)satSteps;

                    Color color1 = Color.HSVToRGB(hue1 / 360f, sat1, value);
                    Color color2 = Color.HSVToRGB(hue2 / 360f, sat1, value);
                    Color color3 = Color.HSVToRGB(hue2 / 360f, sat2, value);
                    Color color4 = Color.HSVToRGB(hue1 / 360f, sat2, value);

                    float x1 = rect.x + h * stepWidth;
                    float x2 = rect.x + (h + 1) * stepWidth;
                    float y1 = rect.y + s * stepHeight;
                    float y2 = rect.y + (s + 1) * stepHeight;

                    // Bottom-left
                    GL.Color(color1);
                    GL.Vertex3(x1, y2, 0);

                    // Bottom-right
                    GL.Color(color2);
                    GL.Vertex3(x2, y2, 0);

                    // Top-right
                    GL.Color(color3);
                    GL.Vertex3(x2, y1, 0);

                    // Top-left
                    GL.Color(color4);
                    GL.Vertex3(x1, y1, 0);
                }
            }

            GL.End();
            GL.PopMatrix();

            // Handle mouse input
            return HandleColorPicking(rect, (mousePos) =>
            {
                float hue = ((mousePos.x - rect.x) / rect.width) * 360f;
                float saturation = 1f - ((mousePos.y - rect.y) / rect.height);
                return Color.HSVToRGB(hue / 360f, saturation, value);
            });
        }

        /// <summary>
        /// Draws a triangle color picker with RGB at the corners
        /// </summary>
        /// <param name="rect">Rectangle to contain the triangle</param>
        /// <returns>Color at mouse position if clicked, otherwise Color.clear</returns>
        public static Color DrawColorTriangle(Rect rect)
        {
            Material mat = GetColorMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.4f;

            // Triangle vertices (equilateral triangle)
            Vector2 topVertex = center + new Vector2(0, -radius);           // Red
            Vector2 bottomLeft = center + new Vector2(-radius * 0.866f, radius * 0.5f);  // Green
            Vector2 bottomRight = center + new Vector2(radius * 0.866f, radius * 0.5f);  // Blue

            // Draw a single triangle with vertex colors - GL handles the interpolation!
            GL.Begin(GL.TRIANGLES);

            GL.Color(Color.red);
            GL.Vertex3(topVertex.x, topVertex.y, 0);

            GL.Color(Color.green);
            GL.Vertex3(bottomLeft.x, bottomLeft.y, 0);

            GL.Color(Color.blue);
            GL.Vertex3(bottomRight.x, bottomRight.y, 0);

            GL.End();
            GL.PopMatrix();

            // Handle mouse input
            return HandleColorPicking(rect, (mousePos) =>
            {
                return GetTriangleColorAtPoint(mousePos, center, radius, topVertex, bottomLeft, bottomRight);
            });
        }

        /// <summary>
        /// Draws a circular color wheel (polygon) with hue around circumference
        /// </summary>
        /// <param name="rect">Rectangle to contain the circle</param>
        /// <param name="saturation">HSV Saturation component 0-1</param>
        /// <param name="value">HSV Value component (brightness) 0-1</param>
        /// <returns>Color at mouse position if clicked, otherwise Color.clear</returns>
        public static Color DrawColorWheel(Rect rect, float saturation = 1f, float value = 1f)
        {
            Material mat = GetColorMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.4f;

            int segments = 60; // Number of segments for smooth circle
            float angleStep = 360f / segments;

            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Color color1 = Color.HSVToRGB(angle1 / 360f, saturation, value);
                Color color2 = Color.HSVToRGB(angle2 / 360f, saturation, value);
                Color centerColor = Color.HSVToRGB(0, 0, value); // White/black center

                Vector2 point1 = center + AngleToVector(angle1) * radius;
                Vector2 point2 = center + AngleToVector(angle2) * radius;

                // Triangle from center to edge
                GL.Color(centerColor);
                GL.Vertex3(center.x, center.y, 0);

                GL.Color(color1);
                GL.Vertex3(point1.x, point1.y, 0);

                GL.Color(color2);
                GL.Vertex3(point2.x, point2.y, 0);
            }

            GL.End();
            GL.PopMatrix();

            // Handle mouse input
            return HandleColorPicking(rect, (mousePos) =>
            {
                Vector2 direction = mousePos - center;
                float distance = direction.magnitude;

                if (distance > radius) return Color.clear;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                float distanceRatio = distance / radius;
                float finalSaturation = saturation * distanceRatio;

                return Color.HSVToRGB(angle / 360f, finalSaturation, value);
            });
        }

        /// <summary>
        /// Draws a full HSV color wheel with saturation/value gradients
        /// </summary>
        /// <param name="rect">Rectangle to contain the wheel</param>
        /// <returns>Color at mouse position if clicked, otherwise Color.clear</returns>
        public static Color DrawFullColorWheel(Rect rect)
        {
            Material mat = GetColorMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.4f;

            int segments = 60;
            int rings = 10;

            GL.Begin(GL.QUADS);

            for (int ring = 0; ring < rings; ring++)
            {
                float innerRadius = (ring / (float)rings) * radius;
                float outerRadius = ((ring + 1) / (float)rings) * radius;

                for (int seg = 0; seg < segments; seg++)
                {
                    float angle1 = (seg / (float)segments) * 360f;
                    float angle2 = ((seg + 1) / (float)segments) * 360f;

                    float saturation = outerRadius / radius;
                    float value = 1f;

                    Color color1 = Color.HSVToRGB(angle1 / 360f, innerRadius / radius, value);
                    Color color2 = Color.HSVToRGB(angle2 / 360f, innerRadius / radius, value);
                    Color color3 = Color.HSVToRGB(angle2 / 360f, saturation, value);
                    Color color4 = Color.HSVToRGB(angle1 / 360f, saturation, value);

                    Vector2 p1 = center + AngleToVector(angle1) * innerRadius;
                    Vector2 p2 = center + AngleToVector(angle2) * innerRadius;
                    Vector2 p3 = center + AngleToVector(angle2) * outerRadius;
                    Vector2 p4 = center + AngleToVector(angle1) * outerRadius;

                    GL.Color(color1);
                    GL.Vertex3(p1.x, p1.y, 0);

                    GL.Color(color2);
                    GL.Vertex3(p2.x, p2.y, 0);

                    GL.Color(color3);
                    GL.Vertex3(p3.x, p3.y, 0);

                    GL.Color(color4);
                    GL.Vertex3(p4.x, p4.y, 0);
                }
            }

            GL.End();
            GL.PopMatrix();

            // Handle mouse input
            return HandleColorPicking(rect, (mousePos) =>
            {
                Vector2 direction = mousePos - center;
                float distance = direction.magnitude;

                if (distance > radius) return Color.clear;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                float saturation = distance / radius;

                return Color.HSVToRGB(angle / 360f, saturation, 1f);
            });
        }

        /// <summary>
        /// Draws a polygon color picker with N sides, each vertex representing a different hue
        /// </summary>
        /// <param name="rect">Rectangle to contain the polygon</param>
        /// <param name="sides">Number of sides for the polygon (minimum 3)</param>
        /// <param name="saturation">HSV Saturation component 0-1</param>
        /// <param name="value">HSV Value component (brightness) 0-1</param>
        /// <param name="centerColor">Color at the center of the polygon</param>
        /// <param name="rotation">Rotation angle in degrees</param>
        /// <returns>Color at mouse position if clicked, otherwise Color.clear</returns>
        public static Color DrawColorPolygon(Rect rect, int sides, float saturation = 1f, float value = 1f,
            Color? centerColor = null, float rotation = 0f)
        {
            if (sides < 3) sides = 3; // Minimum triangle

            Material mat = GetColorMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.4f;

            // Calculate polygon vertices
            Vector2[] vertices = new Vector2[sides];
            Color[] vertexColors = new Color[sides];

            for (int i = 0; i < sides; i++)
            {
                float angle = (360f / sides) * i + rotation;
                vertices[i] = center + AngleToVector(angle) * radius;

                // Distribute hues evenly around the polygon
                float hue = (i / (float)sides);
                vertexColors[i] = Color.HSVToRGB(hue, saturation, value);
            }

            Color finalCenterColor = centerColor ?? Color.HSVToRGB(0, 0, value);

            // Draw polygon using triangulation from center
            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < sides; i++)
            {
                int nextIndex = (i + 1) % sides;

                // Triangle: center -> vertex[i] -> vertex[i+1]
                GL.Color(finalCenterColor);
                GL.Vertex3(center.x, center.y, 0);

                GL.Color(vertexColors[i]);
                GL.Vertex3(vertices[i].x, vertices[i].y, 0);

                GL.Color(vertexColors[nextIndex]);
                GL.Vertex3(vertices[nextIndex].x, vertices[nextIndex].y, 0);
            }

            GL.End();
            GL.PopMatrix();

            // Handle mouse input
            return HandleColorPicking(rect, (mousePos) =>
            {
                return GetPolygonColorAtPoint(mousePos, center, radius, vertices, vertexColors,
                    finalCenterColor, sides);
            });
        }
        
        private static Color _lastSelectedColor;

        /// <summary>
        /// Draws a polygon with custom colors at each vertex
        /// </summary>
        /// <param name="rect">Rectangle to contain the polygon</param>
        /// <param name="sides">Number of sides for the polygon</param>
        /// <param name="vertexColors">Colors for each vertex (array length should match sides)</param>
        /// <param name="centerColor">Color at the center of the polygon</param>
        /// <param name="rotation">Rotation angle in degrees</param>
        /// <returns>Color at mouse position if clicked, otherwise Color.clear</returns>
        public static Color DrawCustomColorPolygon(Rect rect, int sides, Color[] vertexColors,
            Color centerColor, float rotation = 0f)
        {
            if (sides < 3) sides = 3;
            if (vertexColors.Length < sides)
            {
                Debug.LogWarning("Not enough vertex colors provided for polygon");
                return Color.clear;
            }

            Material mat = GetColorMaterial();
            mat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.4f;

            // Calculate polygon vertices
            Vector2[] vertices = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = (360f / sides) * i + rotation;
                vertices[i] = center + AngleToVector(angle) * radius;
            }

            // Draw polygon using triangulation from center
            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < sides; i++)
            {
                int nextIndex = (i + 1) % sides;

                // Triangle: center -> vertex[i] -> vertex[i+1]
                GL.Color(centerColor);
                GL.Vertex3(center.x, center.y, 0);

                GL.Color(vertexColors[i]);
                GL.Vertex3(vertices[i].x, vertices[i].y, 0);

                GL.Color(vertexColors[nextIndex]);
                GL.Vertex3(vertices[nextIndex].x, vertices[nextIndex].y, 0);
            }

            GL.End();
            GL.PopMatrix();

            // Handle mouse input
            return HandleColorPicking(rect, (mousePos) =>
            {
                return GetPolygonColorAtPoint(mousePos, center, radius, vertices, vertexColors,
                    centerColor, sides);
            });
        }

        // Helper methods
        private static Vector2 AngleToVector(float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private static Color HandleColorPicking(Rect rect, System.Func<Vector2, Color> getColorAt)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                return getColorAt(e.mousePosition);
            }
            return Color.clear;
        }

        private static void DrawTessellatedTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color c1, Color c2, Color c3)
        {
            // Simple grid-based tessellation - much more efficient than recursion
            int resolution = 6; // Creates about 36 triangles total instead of trillions!

            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution - i; j++)
                {
                    float u1 = i / (float)resolution;
                    float v1 = j / (float)resolution;
                    float w1 = 1f - u1 - v1;

                    float u2 = (i + 1) / (float)resolution;
                    float v2 = j / (float)resolution;
                    float w2 = 1f - u2 - v2;

                    float u3 = i / (float)resolution;
                    float v3 = (j + 1) / (float)resolution;
                    float w3 = 1f - u3 - v3;

                    if (w1 >= 0 && w2 >= 0 && w3 >= 0)
                    {
                        Vector2 pos1 = w1 * p1 + u1 * p2 + v1 * p3;
                        Vector2 pos2 = w2 * p1 + u2 * p2 + v2 * p3;
                        Vector2 pos3 = w3 * p1 + u3 * p2 + v3 * p3;

                        Color color1 = w1 * c1 + u1 * c2 + v1 * c3;
                        Color color2 = w2 * c1 + u2 * c2 + v2 * c3;
                        Color color3 = w3 * c1 + u3 * c2 + v3 * c3;

                        GL.Color(color1);
                        GL.Vertex3(pos1.x, pos1.y, 0);
                        GL.Color(color2);
                        GL.Vertex3(pos2.x, pos2.y, 0);
                        GL.Color(color3);
                        GL.Vertex3(pos3.x, pos3.y, 0);
                    }

                    // Second triangle in quad (if it fits)
                    if (j < resolution - i - 1)
                    {
                        float u4 = (i + 1) / (float)resolution;
                        float v4 = (j + 1) / (float)resolution;
                        float w4 = 1f - u4 - v4;

                        if (w2 >= 0 && w3 >= 0 && w4 >= 0)
                        {
                            Vector2 pos2 = w2 * p1 + u2 * p2 + v2 * p3;
                            Vector2 pos3 = w3 * p1 + u3 * p2 + v3 * p3;
                            Vector2 pos4 = w4 * p1 + u4 * p2 + v4 * p3;

                            Color color2 = w2 * c1 + u2 * c2 + v2 * c3;
                            Color color3 = w3 * c1 + u3 * c2 + v3 * c3;
                            Color color4 = w4 * c1 + u4 * c2 + v4 * c3;

                            GL.Color(color2);
                            GL.Vertex3(pos2.x, pos2.y, 0);
                            GL.Color(color4);
                            GL.Vertex3(pos4.x, pos4.y, 0);
                            GL.Color(color3);
                            GL.Vertex3(pos3.x, pos3.y, 0);
                        }
                    }
                }
            }

            GL.End();
        }

        private static Color GetTriangleColorAtPoint(Vector2 mousePos, Vector2 center, float radius,
            Vector2 topVertex, Vector2 bottomLeft, Vector2 bottomRight)
        {
            // Calculate barycentric coordinates
            Vector2 v0 = bottomRight - bottomLeft;
            Vector2 v1 = topVertex - bottomLeft;
            Vector2 v2 = mousePos - bottomLeft;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            if (u >= 0 && v >= 0 && u + v <= 1)
            {
                // Point is inside triangle
                float w = 1 - u - v;
                return w * Color.green + u * Color.blue + v * Color.red;
            }

            return Color.clear;
        }

        private static Color GetPolygonColorAtPoint(Vector2 mousePos, Vector2 center, float radius,
            Vector2[] vertices, Color[] vertexColors,
            Color centerColor, int sides)
        {
            // Check if point is within the polygon's bounding circle first
            float distanceFromCenter = Vector2.Distance(mousePos, center);
            if (distanceFromCenter > radius) return Color.clear;

            // Find which triangle (sector) the point is in
            Vector2 direction = (mousePos - center).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            float sectorAngle = 360f / sides;
            int sectorIndex = Mathf.FloorToInt(angle / sectorAngle) % sides;
            int nextSectorIndex = (sectorIndex + 1) % sides;

            // Get the triangle vertices: center, current vertex, next vertex
            Vector2 p1 = center;
            Vector2 p2 = vertices[sectorIndex];
            Vector2 p3 = vertices[nextSectorIndex];

            // Check if point is inside this triangle using barycentric coordinates
            Vector2 v0 = p3 - p1;
            Vector2 v1 = p2 - p1;
            Vector2 v2 = mousePos - p1;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            if (u >= 0 && v >= 0 && u + v <= 1)
            {
                // Point is inside triangle - interpolate colors using barycentric coordinates
                float w = 1 - u - v;

                Color c1 = centerColor;           // Center vertex
                Color c2 = vertexColors[sectorIndex];     // Current vertex  
                Color c3 = vertexColors[nextSectorIndex]; // Next vertex

                var polygonColorAtPoint = w * c1 + v * c2 + u * c3;
                _lastSelectedColor = polygonColorAtPoint;
                return polygonColorAtPoint;
            }
            
            return Color.clear;
        }
    }
}