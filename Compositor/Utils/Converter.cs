using UnityEngine;

namespace Compositor.KK.Utils
{
    public class Converter
    {
        public static unsafe object FastConvert(SocketType from, SocketType to, float[] value)
        {
            if (from == to)
                return value;

            switch (from)
            {
                case SocketType.RGBA when to == SocketType.Alpha:
                {
                    float[] result = new float[value.Length / 4];
                    fixed (float* pBytes = value)
                    {
                        for (var i = 0; i < result.Length; i++)
                        {
                            int pixelIndex = i * 4;
                            var luminance = pBytes[pixelIndex] * 0.2126f + pBytes[pixelIndex + 1] * 0.7152f + pBytes[pixelIndex + 2] * 0.0722f;
                            result[i] = luminance;
                        }
                    }

                    return result;
                }
                case SocketType.Alpha when to == SocketType.RGBA:
                {
                    float[] result = new float[value.Length * 4];
                    fixed (float* pBytes = value)
                    {
                        for (var i = 0; i < value.Length; i++)
                        {
                            var pixelIndex = i * 4;
                            result[pixelIndex] = pBytes[i];
                            result[pixelIndex + 1] = pBytes[i];
                            result[pixelIndex + 2] = pBytes[i];
                            result[pixelIndex + 3] = 255;
                        }
                    }

                    return result;
                }
                case SocketType.RGBA when to == SocketType.Vector:
                {
                    float[] result = new float[value.Length / 4 * 3];
                    fixed (float* pByte = value)
                    {
                        int numPixels = value.Length / 4;
                        for (var i = 0; i < numPixels; i++)
                        {
                            int srcIdx = i * 4;
                            int dstIdx = i * 3;

                            result[dstIdx] = pByte[srcIdx];
                            result[dstIdx + 1] = pByte[srcIdx + 1];
                            result[dstIdx + 2] = pByte[srcIdx + 2];
                        }
                    }

                    return result;
                }
                case SocketType.Vector when to == SocketType.RGBA:
                {
                    float[] result = new float[value.Length / 3 * 4];

                    fixed (float* pByte = value)
                    {
                        int numPixels = value.Length / 3;
                        for (int i = 0; i < numPixels; i++)
                        {
                            int srcIdx = i * 3;
                            int dstIdx = i * 4;

                            result[dstIdx] = pByte[srcIdx];
                            result[dstIdx + 1] = pByte[srcIdx + 1];
                            result[dstIdx + 2] = pByte[srcIdx + 2];
                            result[dstIdx + 3] = 255;
                        }
                    }

                    return result;
                }
                case SocketType.Alpha when to == SocketType.Vector:
                {
                    float[] result = new float[value.Length * 3];
                    fixed (float* pByte = value)
                    {
                        int numPixels = value.Length;
                        for (int i = 0; i < numPixels; i++)
                        {
                            int dstIdx = i * 3;
                            result[dstIdx] = pByte[i];
                            result[dstIdx + 1] = 0;
                            result[dstIdx + 2] = 0;
                        }
                    }

                    return result;
                }
                case SocketType.Vector when to == SocketType.Alpha:
                {
                    float[] result = new float[value.Length / 3];
                    fixed (float* pByte = value)
                    {
                        for (int i = 0; i < result.Length; i++)
                        {
                            int srcIdx = i * 3;
                            result[i] = pByte[srcIdx];
                        }
                    }

                    return result;
                }
                default:
                    return null;
            }

        }
    }
}