using System.Collections.Generic;
using UnityEngine;

namespace Prototype
{
    public class Curve
    {
        /// <summary>
        /// Performs Catmull-Rom spline interpolation between four points to calculate a position at a specific interpolation value.
        /// </summary>
        /// <param name="p0">The control point preceding the starting point of the interpolation segment.</param>
        /// <param name="p1">The starting point of the interpolation segment.</param>
        /// <param name="p2">The ending point of the interpolation segment.</param>
        /// <param name="p3">The control point following the ending point of the interpolation segment.</param>
        /// <param name="t">The interpolation factor, ranging from 0 (start) to 1 (end), that determines the position along the segment.</param>
        /// <returns>A <see cref="Vector2"/> representing the interpolated position along the Catmull-Rom spline.</returns>
        public static Vector2 CatmullRomInterpolate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            Vector2 result = 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );

            return result;
        }

        public static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
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
        /// Generates a smooth spline curve by interpolating input points using the Catmull-Rom algorithm.
        /// </summary>
        /// <param name="points">An array of input points to form the base of the spline.</param>
        /// <param name="smoothness">The number of interpolated points between each pair of input points. Higher values result in smoother curves.</param>
        /// <returns>An array of <see cref="Vector2"/> representing the smoothed spline including the original input points.</returns>
        public static Vector2[] SplineVisual(Vector2[] points, int smoothness)
        {
            if (points.Length < 3) return points;

            var smoothPoints = new List<Vector2>();

            smoothPoints.Add(points[0]);

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 p0, p1, p2, p3;

                p1 = points[i];
                p2 = points[i + 1];

                if (i == 0)
                {
                    p0 = p1 + (p1 - p2);
                }
                else
                {
                    p0 = points[i - 1];
                }

                if (i == points.Length - 2)
                {
                    p3 = p2 + (p2 - p1);
                }
                else
                {
                    p3 = points[i + 2];
                }

                for (int j = 1; j <= smoothness; j++)
                {
                    float t = j / (float)(smoothness + 1);
                    Vector2 interpolatedPoint = CatmullRomInterpolate(p0, p1, p2, p3, t);
                    smoothPoints.Add(interpolatedPoint);
                }

                if (i < points.Length - 2)
                {
                    smoothPoints.Add(p2);
                }
            }

            smoothPoints.Add(points[points.Length - 1]);

            return smoothPoints.ToArray();
        }

        /// <summary>
        /// Increases the number of points in the given polyline to match the specified number of segments, optionally smoothing the result.
        /// </summary>
        /// <param name="poly">The original polyline represented as a list of <see cref="UnityEngine.Vector3"/>.</param>
        /// <param name="segments">The desired number of segments in the resulting polyline. Must be greater than or equal to the number of points in the original polyline.</param>
        /// <param name="smooth">Determines whether smooth interpolation is applied to the new points in the polyline. Defaults to false.</param>
        /// <returns>
        /// A new polyline with the specified number of segments, represented as a list of <see cref="UnityEngine.Vector3"/>.
        /// </returns>
        public static IList<Vector3> IncreasePoly(IList<Vector3> poly, int segments, bool smooth = false)
        {
            if (poly.Count >= segments)
                return poly;

            Vector3[] result = new Vector3[segments];
            float step = (poly.Count - 1) / (float)(segments - 1);

            for (int i = 0; i < segments; i++)
            {
                float index = i * step;
                int floor = Mathf.FloorToInt(index);
                float t = index - floor;

                if (floor >= poly.Count - 1)
                {
                    result[i] = poly[poly.Count - 1];
                }
                else if (smooth && floor > 0 && floor < poly.Count - 2)
                {
                    Vector3 p0 = poly[floor - 1];
                    Vector3 p1 = poly[floor];
                    Vector3 p2 = poly[floor + 1];
                    Vector3 p3 = poly[floor + 2];

                    float tSquared = t * t;
                    float tCubed = tSquared * t;

                    float q1 = -tCubed + 2.0f * tSquared - t;
                    float q2 = 3.0f * tCubed - 5.0f * tSquared + 2.0f;
                    float q3 = -3.0f * tCubed + 4.0f * tSquared + t;
                    float q4 = tCubed - tSquared;

                    result[i] = 0.5f * (q1 * p0 + q2 * p1 + q3 * p2 + q4 * p3);
                }
                else
                {
                    result[i] = UnityEngine.Vector3.Lerp(poly[floor], poly[floor + 1], t);
                }
            }

            return result;
        }

        /// <summary>
        /// Reduces a given polygon's number of vertices to the specified number of segments.
        /// </summary>
        /// <param name="poly">The input list of vertices representing the polygon.</param>
        /// <param name="segments">The number of vertices to reduce the polygon to.</param>
        /// <returns>A new list of vertices representing the reduced polygon.</returns>
        public static IList<Vector3> ReducePoly(IList<Vector3> poly, int segments)
        {
            if (poly.Count <= segments)
                return poly;

            Vector3[] result = new Vector3[segments];
            float step = (poly.Count - 1) / (float)(segments - 1);

            for (int i = 0; i < segments; i++)
            {
                float index = i * step;
                int floor = Mathf.FloorToInt(index);
                float t = index - floor;

                if (floor >= poly.Count - 1)
                {
                    result[i] = poly[poly.Count - 1];
                }
                else
                {
                    result[i] = UnityEngine.Vector3.Lerp(poly[floor], poly[floor + 1], t);
                }
            }

            return result;
        }
    }
}