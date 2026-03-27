using System;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Standard biquad IIR filter using Audio EQ Cookbook formulas.
    /// </summary>
    public class Biquad
    {
        public enum Type { LowPass, HighPass, BandPass, PeakingEQ, LowShelf, HighShelf }

        private float _b0, _b1, _b2, _a1, _a2;
        private float _x1, _x2, _y1, _y2;

        public void Configure(Type type, float sampleRate, float freq, float q, float dbGain = 0f)
        {
            float w0 = 2f * (float)Math.PI * freq / sampleRate;
            float cosw0 = (float)Math.Cos(w0);
            float sinw0 = (float)Math.Sin(w0);
            float alpha = sinw0 / (2f * q);

            float a0 = 1f, a1 = 0f, a2 = 0f;
            float b0 = 1f, b1 = 0f, b2 = 0f;

            switch (type)
            {
                case Type.LowPass:
                    b0 = (1f - cosw0) / 2f;
                    b1 = 1f - cosw0;
                    b2 = (1f - cosw0) / 2f;
                    a0 = 1f + alpha;
                    a1 = -2f * cosw0;
                    a2 = 1f - alpha;
                    break;

                case Type.HighPass:
                    b0 = (1f + cosw0) / 2f;
                    b1 = -(1f + cosw0);
                    b2 = (1f + cosw0) / 2f;
                    a0 = 1f + alpha;
                    a1 = -2f * cosw0;
                    a2 = 1f - alpha;
                    break;

                case Type.BandPass:
                    b0 = alpha;
                    b1 = 0f;
                    b2 = -alpha;
                    a0 = 1f + alpha;
                    a1 = -2f * cosw0;
                    a2 = 1f - alpha;
                    break;

                case Type.PeakingEQ:
                {
                    float A = (float)Math.Pow(10.0, dbGain / 40.0);
                    b0 = 1f + alpha * A;
                    b1 = -2f * cosw0;
                    b2 = 1f - alpha * A;
                    a0 = 1f + alpha / A;
                    a1 = -2f * cosw0;
                    a2 = 1f - alpha / A;
                    break;
                }

                case Type.LowShelf:
                {
                    float A = (float)Math.Pow(10.0, dbGain / 40.0);
                    float twoSqrtAAlpha = 2f * (float)Math.Sqrt(A) * alpha;
                    b0 = A * ((A + 1f) - (A - 1f) * cosw0 + twoSqrtAAlpha);
                    b1 = 2f * A * ((A - 1f) - (A + 1f) * cosw0);
                    b2 = A * ((A + 1f) - (A - 1f) * cosw0 - twoSqrtAAlpha);
                    a0 = (A + 1f) + (A - 1f) * cosw0 + twoSqrtAAlpha;
                    a1 = -2f * ((A - 1f) + (A + 1f) * cosw0);
                    a2 = (A + 1f) + (A - 1f) * cosw0 - twoSqrtAAlpha;
                    break;
                }

                case Type.HighShelf:
                {
                    float A = (float)Math.Pow(10.0, dbGain / 40.0);
                    float twoSqrtAAlpha = 2f * (float)Math.Sqrt(A) * alpha;
                    b0 = A * ((A + 1f) + (A - 1f) * cosw0 + twoSqrtAAlpha);
                    b1 = -2f * A * ((A - 1f) + (A + 1f) * cosw0);
                    b2 = A * ((A + 1f) + (A - 1f) * cosw0 - twoSqrtAAlpha);
                    a0 = (A + 1f) - (A - 1f) * cosw0 + twoSqrtAAlpha;
                    a1 = 2f * ((A - 1f) - (A + 1f) * cosw0);
                    a2 = (A + 1f) - (A - 1f) * cosw0 - twoSqrtAAlpha;
                    break;
                }
            }

            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public float Process(float x)
        {
            float y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
            _x2 = _x1;
            _x1 = x;
            _y2 = _y1;
            _y1 = y;
            return y;
        }

        public void Reset()
        {
            _x1 = _x2 = _y1 = _y2 = 0f;
        }
    }
}