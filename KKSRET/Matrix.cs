using System;
using UnityEngine;

namespace KKSRET
{
    public struct Matrix3x3
    {
        public float m00, m01, m02;
        public float m10, m11, m12;
        public float m20, m21, m22;

        public static Matrix3x3 identity => new Matrix3x3
        {
            m00 = 1, m01 = 0, m02 = 0,
            m10 = 0, m11 = 1, m12 = 0,
            m20 = 0, m21 = 0, m22 = 1
        };

        public Matrix3x3(float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;
            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
        }

        public Vector3 GetColumn(int index)
        {
            switch (index)
            {
                case 0: return new Vector3(m00, m10, m20);
                case 1: return new Vector3(m01, m11, m21);
                case 2: return new Vector3(m02, m12, m22);
                default: throw new IndexOutOfRangeException();
            }
        }

        public void SetColumn(int index, Vector3 column)
        {
            switch (index)
            {
                case 0:
                    m00 = column.x;
                    m10 = column.y;
                    m20 = column.z;
                    break;
                case 1:
                    m01 = column.x;
                    m11 = column.y;
                    m21 = column.z;
                    break;
                case 2:
                    m02 = column.x;
                    m12 = column.y;
                    m22 = column.z;
                    break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    public static class MatrixRotation
    {
        public static Matrix4x4 Rotation4x4(float angle, char axis)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            Matrix4x4 matrix = Matrix4x4.identity;

            switch (axis)
            {
                case 'X':
                case 'x':
                    matrix.SetColumn(0, new Vector4(1,    0,    0, 0));
                    matrix.SetColumn(1, new Vector4(0,  cos,  sin, 0));
                    matrix.SetColumn(2, new Vector4(0, -sin,  cos, 0));
                    matrix.SetColumn(3, new Vector4(0,    0,    0, 1));
                    break;

                case 'Y':
                case 'y':
                    matrix.SetColumn(0, new Vector4( cos, 0, -sin, 0));
                    matrix.SetColumn(1, new Vector4(   0, 1,    0, 0));
                    matrix.SetColumn(2, new Vector4( sin, 0,  cos, 0));
                    matrix.SetColumn(3, new Vector4(   0, 0,    0, 1));
                    break;

                case 'Z':
                case 'z':
                    matrix.SetColumn(0, new Vector4( cos, sin, 0, 0));
                    matrix.SetColumn(1, new Vector4(-sin, cos, 0, 0));
                    matrix.SetColumn(2, new Vector4(   0,   0, 1, 0));
                    matrix.SetColumn(3, new Vector4(   0,   0, 0, 1));
                    break;

                default:
                    throw new ArgumentException($"Invalid axis: {axis}");
            }

            return matrix;
        }

        public static Matrix3x3 Rotation3x3(float angle, char axis)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            Matrix3x3 matrix = Matrix3x3.identity;

            switch (axis)
            {
                case 'X':
                case 'x':
                    matrix.SetColumn(0, new Vector3(1,    0,    0));
                    matrix.SetColumn(1, new Vector3(0,  cos,  sin));
                    matrix.SetColumn(2, new Vector3(0, -sin,  cos));
                    break;

                case 'Y':
                case 'y':
                    matrix.SetColumn(0, new Vector3( cos, 0, -sin));
                    matrix.SetColumn(1, new Vector3(   0, 1,    0));
                    matrix.SetColumn(2, new Vector3( sin, 0,  cos));
                    break;

                case 'Z':
                case 'z':
                    matrix.SetColumn(0, new Vector3( cos, sin, 0));
                    matrix.SetColumn(1, new Vector3(-sin, cos, 0));
                    matrix.SetColumn(2, new Vector3(   0,   0, 1));
                    break;

                default:
                    throw new ArgumentException($"Invalid axis: {axis}");
            }

            return matrix;
        }


        public static Matrix4x4 Matrix3x3ToMatrix4x4(Matrix3x3 m)
        {
            Matrix4x4 result = Matrix4x4.identity;
            result.m00 = m.m00;
            result.m01 = m.m01;
            result.m02 = m.m02;
            result.m10 = m.m10;
            result.m11 = m.m11;
            result.m12 = m.m12;
            result.m20 = m.m20;
            result.m21 = m.m21;
            result.m22 = m.m22;
            return result;
        }
    }
}