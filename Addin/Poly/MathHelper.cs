using UnityEngine;

namespace Addin
{
    public class MathHelper
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
        /// Remaps a value from one range to another, without clamping.
        /// Equivalent to Arduino's map() but for floats.
        /// </summary>
        /// <param name="value">The value to remap.</param>
        /// <param name="fromMin">Lower bound of the input range.</param>
        /// <param name="fromMax">Upper bound of the input range.</param>
        /// <param name="toMin">Lower bound of the output range.</param>
        /// <param name="toMax">Upper bound of the output range.</param>
        /// <returns>The remapped value, which may exceed [toMin, toMax] if value is outside [fromMin, fromMax].</returns>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        /// <summary>
        /// Remaps a value from one range to another, clamped to the output range.
        /// </summary>
        public static float RemapClamped(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.Clamp(Remap(value, fromMin, fromMax, toMin, toMax), toMin, toMax);
        }

        /// <summary>
        /// Computes the signed area of a polygon using the shoelace formula.
        /// A counter-clockwise winding returns a positive area; clockwise returns negative.
        /// </summary>
        /// <param name="polygonPoints">Vertices of the polygon in order.</param>
        /// <returns>Signed area of the polygon.</returns>
        public static float PolygonSignedArea(Vector2[] polygonPoints)
        {
            float area = 0f;
            int n = polygonPoints.Length;

            for (int i = 0, j = n - 1; i < n; j = i++)
                area += (polygonPoints[j].x + polygonPoints[i].x) * (polygonPoints[j].y - polygonPoints[i].y);

            return area * 0.5f;
        }

        /// <summary>
        /// Computes the unsigned area of a polygon.
        /// </summary>
        public static float PolygonArea(Vector2[] polygonPoints)
        {
            return Mathf.Abs(PolygonSignedArea(polygonPoints));
        }

        /// <summary>
        /// Computes the centroid (geometric centre) of a polygon.
        /// </summary>
        /// <param name="polygonPoints">Vertices of the polygon in order.</param>
        /// <returns>The centroid as a Vector2.</returns>
        public static Vector2 PolygonCentroid(Vector2[] polygonPoints)
        {
            float cx = 0f, cy = 0f;
            int n = polygonPoints.Length;
            float signedArea = PolygonSignedArea(polygonPoints);

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float cross = polygonPoints[j].x * polygonPoints[i].y - polygonPoints[i].x * polygonPoints[j].y;
                cx += (polygonPoints[j].x + polygonPoints[i].x) * cross;
                cy += (polygonPoints[j].y + polygonPoints[i].y) * cross;
            }

            float factor = 1f / (6f * signedArea);
            return new Vector2(cx * factor, cy * factor);
        }

        /// <summary>
        /// Returns whether a polygon's vertices are wound counter-clockwise.
        /// </summary>
        public static bool IsCounterClockwise(Vector2[] polygonPoints)
        {
            return PolygonSignedArea(polygonPoints) > 0f;
        }

        /// <summary>
        /// Returns whether all interior angles of a polygon are less than 180°, i.e. the polygon is convex.
        /// Works for both winding orders.
        /// </summary>
        public static bool IsConvex(Vector2[] polygonPoints)
        {
            int n = polygonPoints.Length;
            if (n < 3) return false;

            float sign = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygonPoints[(i + 1) % n] - polygonPoints[i];
                Vector2 b = polygonPoints[(i + 2) % n] - polygonPoints[(i + 1) % n];
                float cross = a.x * b.y - a.y * b.x;

                if (cross != 0f)
                {
                    if (sign == 0f)
                        sign = Mathf.Sign(cross);
                    else if (Mathf.Sign(cross) != sign)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes the smallest axis-aligned bounding box that contains all polygon vertices.
        /// </summary>
        public static Rect PolygonBounds(Vector2[] polygonPoints)
        {
            float xMin = polygonPoints[0].x, xMax = polygonPoints[0].x;
            float yMin = polygonPoints[0].y, yMax = polygonPoints[0].y;

            for (int i = 1; i < polygonPoints.Length; i++)
            {
                if (polygonPoints[i].x < xMin) xMin = polygonPoints[i].x;
                if (polygonPoints[i].x > xMax) xMax = polygonPoints[i].x;
                if (polygonPoints[i].y < yMin) yMin = polygonPoints[i].y;
                if (polygonPoints[i].y > yMax) yMax = polygonPoints[i].y;
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        /// <summary>
        /// Returns the closest point on a finite line segment to a given point.
        /// </summary>
        /// <param name="point">The query point.</param>
        /// <param name="segA">Start of the segment.</param>
        /// <param name="segB">End of the segment.</param>
        /// <returns>The closest point on the segment.</returns>
        public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 segA, Vector2 segB)
        {
            Vector2 ab = segB - segA;
            float t = Vector2.Dot(point - segA, ab) / ab.sqrMagnitude;
            return segA + Mathf.Clamp01(t) * ab;
        }

        /// <summary>
        /// Returns the perpendicular distance from a point to an infinite line defined by two points.
        /// </summary>
        public static float PointToLineDistance(Vector2 point, Vector2 lineA, Vector2 lineB)
        {
            Vector2 ab = lineB - lineA;
            return Mathf.Abs(ab.x * (lineA.y - point.y) - ab.y * (lineA.x - point.x)) / ab.magnitude;
        }

        /// <summary>
        /// Tests whether two finite line segments intersect and, if so, outputs the intersection point.
        /// </summary>
        /// <param name="a1">Start of the first segment.</param>
        /// <param name="a2">End of the first segment.</param>
        /// <param name="b1">Start of the second segment.</param>
        /// <param name="b2">End of the second segment.</param>
        /// <param name="intersection">The intersection point, or <c>Vector2.zero</c> if the segments do not intersect.</param>
        /// <returns>True if the segments intersect.</returns>
        public static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
        {
            Vector2 d1 = a2 - a1;
            Vector2 d2 = b2 - b1;
            float cross = d1.x * d2.y - d1.y * d2.x;

            if (Mathf.Approximately(cross, 0f))
            {
                intersection = Vector2.zero;
                return false;
            }

            Vector2 delta = b1 - a1;
            float t = (delta.x * d2.y - delta.y * d2.x) / cross;
            float u = (delta.x * d1.y - delta.y * d1.x) / cross;

            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                intersection = a1 + t * d1;
                return true;
            }

            intersection = Vector2.zero;
            return false;
        }
        
        /// <summary>
        /// Returns the signed angle in degrees from the positive X axis to the vector
        /// pointing from <paramref name="from"/> to <paramref name="to"/>.
        /// Range: (-180, 180].
        /// </summary>
        public static float DirectionAngle(Vector2 from, Vector2 to)
        {
            return Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Converts a direction angle (degrees, 0 = right) to a normalised Vector2.
        /// </summary>
        public static Vector2 AngleToDirection(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>
        /// Wraps an angle in degrees to the range [0, 360).
        /// </summary>
        public static float WrapAngle(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        /// <summary>
        /// Returns the shortest signed difference between two angles in degrees.
        /// Result is in the range (-180, 180].
        /// </summary>
        public static float DeltaAngle(float from, float to)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f)  delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }

        /// <summary>
        /// Applies a critically-damped spring to smoothly drive a float value
        /// toward a target. Unlike <c>Mathf.SmoothDamp</c> this formulation is
        /// framerate-independent and never overshoots.
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="target">The target value to approach.</param>
        /// <param name="velocity">A reference velocity that is maintained across calls.</param>
        /// <param name="halfLife">
        /// Time in seconds for the gap to shrink by half. Smaller values snap faster.
        /// </param>
        /// <param name="deltaTime">Time elapsed since the last call (use <c>Time.deltaTime</c>).</param>
        /// <returns>The new current value after one time step.</returns>
        public static float SpringDamp(float current, float target, ref float velocity, float halfLife, float deltaTime)
        {
            // omega = ln(2) / halfLife
            float omega = 0.6931471805599453f / Mathf.Max(halfLife, 1e-5f);
            float x = current - target;
            float y = velocity + omega * x;
            float factor = Mathf.Exp(-omega * deltaTime);
            velocity = (velocity - omega * y * deltaTime) * factor;
            return target + (x + y * deltaTime) * factor;
        }

        /// <summary>
        /// Vector2 overload of <see cref="SpringDamp"/>.
        /// </summary>
        public static Vector2 SpringDamp(Vector2 current, Vector2 target, ref Vector2 velocity, float halfLife, float deltaTime)
        {
            return new Vector2(
                SpringDamp(current.x, target.x, ref velocity.x, halfLife, deltaTime),
                SpringDamp(current.y, target.y, ref velocity.y, halfLife, deltaTime)
            );
        }

        /// <summary>
        /// Vector3 overload of <see cref="SpringDamp"/>.
        /// </summary>
        public static Vector3 SpringDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float halfLife, float deltaTime)
        {
            return new Vector3(
                SpringDamp(current.x, target.x, ref velocity.x, halfLife, deltaTime),
                SpringDamp(current.y, target.y, ref velocity.y, halfLife, deltaTime),
                SpringDamp(current.z, target.z, ref velocity.z, halfLife, deltaTime)
            );
        }
    }
}