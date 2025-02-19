using JetBrains.Annotations;
using UnityEngine;

namespace SmartRectV0
{
    public struct BezierTemplate
    {
        public Vector3 Start;
        public Vector3 End;
        public Vector3 Control1;
        public Vector3 Control2;
    }

    public static class Beziers
    {
        public static Vector3 Vector3(BezierTemplate b, float f)
        {
            return Vector3(b.Start, b.Control1, b.Control2, b.End, f);
        } 
        
        private static Vector3 Vector3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float num = 1f - t;
            return num * num * num * p0 + 3f * num * num * t * p1 + 3f * num * t * t * p2 + t * t * t * p3;
        }

        [UsedImplicitly]
        public static BezierTemplate LinearTemplate { get; } = new BezierTemplate
        {
            Start = Vector2.zero,
            Control1 = Vector2.zero,
            Control2 = Vector2.one,
            End = Vector2.one
        };

        [UsedImplicitly]
        public static BezierTemplate EaseTemplate { get; } = new BezierTemplate
        {
            Start = Vector2.zero,
            End = Vector2.one,
            Control1 = new Vector2(0.25f, 0.1f),
            Control2 = new Vector2(0.25f, 1f)
        };

        [UsedImplicitly]
        public static BezierTemplate EaseInTemplate { get; } = new BezierTemplate
        {
            Start = Vector2.zero,
            End = Vector2.one,
            Control1 = new Vector2(0.42f, 0f),
            Control2 = Vector2.one
        };

        [UsedImplicitly]
        public static BezierTemplate EaseOutTemplate { get; } = new BezierTemplate
        {
            Start = Vector2.zero,
            End = Vector2.one,
            Control1 = Vector2.zero,
            Control2 = new Vector2(0.58f, 1f)
        };

        [UsedImplicitly]
        public static BezierTemplate EaseInOutTemplate { get; } = new BezierTemplate
        {
            Start = Vector2.zero,
            End = Vector2.one,
            Control1 = new Vector2(0.42f, 0),
            Control2 = new Vector2(0.58f, 1)
        };
    }
}