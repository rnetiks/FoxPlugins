using System;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Cooley-Tukey radix-2 FFT. Input length must be a power of 2.
    /// </summary>
    public static class FFT
    {
        public static void Forward(float[] real, float[] imag, int n)
        {
            BitReverse(real, imag, n);
            Butterfly(real, imag, n, false);
        }

        public static void Inverse(float[] real, float[] imag, int n)
        {
            BitReverse(real, imag, n);
            Butterfly(real, imag, n, true);

            float scale = 1f / n;
            for (int i = 0; i < n; i++)
            {
                real[i] *= scale;
                imag[i] *= scale;
            }
        }

        private static void Butterfly(float[] real, float[] imag, int n, bool inverse)
        {
            float sign = inverse ? 1f : -1f;

            for (int size = 2; size <= n; size *= 2)
            {
                int half = size / 2;
                float angle = sign * 2f * (float)Math.PI / size;
                float wR = (float)Math.Cos(angle);
                float wI = (float)Math.Sin(angle);

                for (int i = 0; i < n; i += size)
                {
                    float curR = 1f, curI = 0f;
                    for (int j = 0; j < half; j++)
                    {
                        int a = i + j;
                        int b = a + half;

                        float tR = curR * real[b] - curI * imag[b];
                        float tI = curR * imag[b] + curI * real[b];

                        real[b] = real[a] - tR;
                        imag[b] = imag[a] - tI;
                        real[a] += tR;
                        imag[a] += tI;

                        float newR = curR * wR - curI * wI;
                        curI = curR * wI + curI * wR;
                        curR = newR;
                    }
                }
            }
        }

        private static void BitReverse(float[] real, float[] imag, int n)
        {
            int bits = 0;
            int tmp = n;
            while (tmp > 1) { tmp >>= 1; bits++; }

            for (int i = 1; i < n - 1; i++)
            {
                int j = ReverseBits(i, bits);
                if (j > i)
                {
                    float tr = real[i]; real[i] = real[j]; real[j] = tr;
                    float ti = imag[i]; imag[i] = imag[j]; imag[j] = ti;
                }
            }
        }

        private static int ReverseBits(int val, int bits)
        {
            int result = 0;
            for (int i = 0; i < bits; i++)
            {
                result = (result << 1) | (val & 1);
                val >>= 1;
            }
            return result;
        }

        public static int NextPowerOf2(int n)
        {
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }
    }
}