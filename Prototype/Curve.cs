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

        /// <summary>
        /// Generates a smooth spline curve by interpolating input points using the Catmull-Rom algorithm.
        /// </summary>
        /// <param name="points">An array of input points to form the base of the spline.</param>
        /// <param name="smoothness">The number of interpolated points between each pair of input points. Higher values result in smoother curves.</param>
        /// <returns>An array of <see cref="Vector2"/> representing the smoothed spline including the original input points.</returns>
        public static Vector2[] Spline(Vector2[] points, int smoothness)
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
    }
}