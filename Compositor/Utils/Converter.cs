using UnityEngine;

namespace Compositor.KK.Utils
{
    public class Converter
    {
        public static unsafe object FastConvert(SocketType from, SocketType to, object value)
        {
            if (from == to)
                return value;

            if (from == SocketType.RGBA && to == SocketType.A)
            {
                byte[] bytes = (byte[])value;
                byte[] result = new byte[bytes.Length / 4];

                fixed (byte* pBytes = bytes)
                {
                    for (var i = 0; i < result.Length; i++)
                    {
                        int pixelIndex = i * 4;
                        var luminance = pBytes[pixelIndex] * 0.2126f + pBytes[pixelIndex + 1] * 0.7152f + pBytes[pixelIndex + 2] * 0.0722f;
                        result[i] = (byte)Mathf.Clamp(Mathf.Round(luminance), 0, 255);
                    }
                }

                return result;
            }

            if (from == SocketType.A && to == SocketType.RGBA)
            {
                var bytes = (byte[])value;
                var result = new byte[bytes.Length * 4];
                fixed (byte* pBytes = bytes)
                {
                    for (var i = 0; i < bytes.Length; i++)
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

            if (from == SocketType.RGBA && to == SocketType.Vector)
            {
                byte[] bytes = (byte[])value;
                byte[] result = new byte[bytes.Length / 4 * 3];
                fixed (byte* pByte = bytes)
                {
                    int numPixels = bytes.Length / 4;
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

            if (from == SocketType.Vector && to == SocketType.RGBA)
            {
                byte[] bytes = (byte[])value;
                byte[] result = new byte[bytes.Length / 3 * 4];

                fixed (byte* pByte = bytes)
                {
                    int numPixels = bytes.Length / 3;
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

            if (from == SocketType.A && to == SocketType.Vector)
            {
                byte[] bytes = (byte[])value;
                byte[] result = new byte[bytes.Length * 3];
                fixed (byte* pByte = bytes)
                {
                    int numPixels = bytes.Length;
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

            if (from == SocketType.Vector && to == SocketType.A)
            {
                byte[] bytes = (byte[])value;
                byte[] result = new byte[bytes.Length / 3];

                fixed (byte* pByte = bytes)
                {
                    for (int i = 0; i < result.Length; i++)
                    {
                        int srcIdx = i * 3;
                        result[i] = pByte[srcIdx];
                    }
                }

                return result;
            }
            
            return null;
        }
    }
}