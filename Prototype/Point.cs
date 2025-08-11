using UnityEngine;

namespace Prototype
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
    }
}