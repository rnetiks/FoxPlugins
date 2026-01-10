using UnityEngine;

namespace TiledRenderer
{
    internal static class MatrixHelper
    {
        public static Matrix4x4 CreateFrustum(float left, float right, float bottom, float top, float near, float far)
        {
            Matrix4x4 m = new Matrix4x4();

            float x = (2.0f * near) / (right - left);
            float y = (2.0f * near) / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(far + near) / (far - near);
            float d = -(2.0f * far * near) / (far - near);

            m[0, 0] = x;    m[0, 1] = 0;    m[0, 2] = a;    m[0, 3] = 0;
            m[1, 0] = 0;    m[1, 1] = y;    m[1, 2] = b;    m[1, 3] = 0;
            m[2, 0] = 0;    m[2, 1] = 0;    m[2, 2] = c;    m[2, 3] = d;
            m[3, 0] = 0;    m[3, 1] = 0;    m[3, 2] = -1;   m[3, 3] = 0;

            return m;
        }
    }
}