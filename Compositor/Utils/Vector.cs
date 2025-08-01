using UnityEngine;

namespace Compositor.KKS.Utils
{
    public class Vector
    {
        public static bool IsPointInTriangle(float px, float py, float x0, float y0, float x1, float y1, float x2, float y2)
        {
            float denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
            if (Mathf.Abs(denom) < 0.0001f) return false;

            float alpha = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom;
            float beta = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom;
            float gamma = 1 - alpha - beta;
            return alpha >= 0 && beta >= 0 && gamma >= 0;
        }
        
        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return IsPointInTriangle(p.x, p.y, a.x, a.y, b.x, b.y, c.x, c.y);
        }
    }
}