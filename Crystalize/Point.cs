using System;
using UnityEngine;

namespace Crystalize
{
    public class Point
    {
        /// <summary>
        /// Determines whether a given 2D point is located within a specified polygon defined by an array of points.
        /// </summary>
        /// <param name="point">The 2D point to check, represented as a Vector2.</param>
        /// <param name="polygonPoints">An array of Vector2 points defining the vertices of the polygon. The points should be ordered either clockwise or counterclockwise.</param>
        /// <returns>True if the point is within the polygon; otherwise, false.</returns>
        public static bool IsPointInPolygon(Vector2 point, Vector2[] polygonPoints)
        {
            int j = polygonPoints.Length - 1;
            bool inside = false;

            for (int i = 0; i < polygonPoints.Length; j = i++)
            {
                if ((polygonPoints[i].y > point.y) != (polygonPoints[j].y > point.y) &&
                    point.x < (polygonPoints[j].x - polygonPoints[i].x) * (point.y - polygonPoints[i].y) /
                    (polygonPoints[j].y - polygonPoints[i].y) + polygonPoints[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Determines whether a given 3D point lies within a specified rectangular bounding box in 2D space.
        /// </summary>
        /// <param name="point">The point to check, represented as a Vector3. The method considers only the x and y components for the check, ensuring the z component is greater than 0.</param>
        /// <param name="boundingBox">The rectangular bounding box, defined by its x and y position (top-left corner) and its width and height.</param>
        /// <returns>True if the point lies within the bounding box; otherwise, false.</returns>
        public static bool IsPointInRect(Vector3 point, Rect boundingBox)
        {
            return point.z > 0 &&
                   point.x >= boundingBox.x &&
                   point.x <= boundingBox.x + boundingBox.width &&
                   point.y >= boundingBox.y &&
                   point.y <= boundingBox.y + boundingBox.height;
        }

        /// <summary>
        /// Determines whether a given 2D point is located within a specified triangle defined by its three vertices.
        /// </summary>
        /// <param name="px">The x-coordinate of the 2D point to check.</param>
        /// <param name="py">The y-coordinate of the 2D point to check.</param>
        /// <param name="x0">The x-coordinate of the first vertex of the triangle.</param>
        /// <param name="y0">The y-coordinate of the first vertex of the triangle.</param>
        /// <param name="x1">The x-coordinate of the second vertex of the triangle.</param>
        /// <param name="y1">The y-coordinate of the second vertex of the triangle.</param>
        /// <param name="x2">The x-coordinate of the third vertex of the triangle.</param>
        /// <param name="y2">The y-coordinate of the third vertex of the triangle.</param>
        /// <returns>True if the point is within the triangle; otherwise, false.</returns>
        [Obsolete("Use IsPointInPolygon instead.")]
        public static bool IsPointInTriangle(float px, float py, float x0, float y0, float x1, float y1, float x2, float y2)
        {
            float denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
            if (Mathf.Abs(denom) < 0.0001f) return false;

            float alpha = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom;
            float beta = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom;
            float gamma = 1 - alpha - beta;
            return alpha >= 0 && beta >= 0 && gamma >= 0;
        }
    }
}