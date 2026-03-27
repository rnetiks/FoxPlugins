using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    public class PitchShifter : AudioFilterBase
    {
        public override string Name { get; set; } = "Pitch Shift";
        public override string Group { get; set; } = "Pitch & Speed";

        private string _semitonesInput = "2";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Semitones:", GUILayout.Width(80));
            _semitonesInput = GUILayout.TextField(_semitonesInput, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Positive = higher pitch, negative = lower.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_semitonesInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float semitones))
                return;

            float pitchFactor = Mathf.Pow(2f, semitones / 12f);
            if (Mathf.Abs(pitchFactor - 1f) < 0.001f) return;

            int channels = data.Channels;
            int totalFrames = data.Samples.Length / channels;
            float[] output = new float[data.Samples.Length];

            const int grainSize = 2048;
            const int halfGrain = grainSize / 2;

            for (int ch = 0; ch < channels; ch++)
            {
                for (int i = 0; i < totalFrames; i++)
                {
                    if ((i & 4095) == 0)
                        ReportProgress(((float)ch / channels) + ((float)i / totalFrames / channels));
                    int phase1 = i % grainSize;
                    int phase2 = (i + halfGrain) % grainSize;

                    float w1 = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * phase1 / grainSize));
                    float w2 = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * phase2 / grainSize));

                    double readPos1 = (i - phase1) + phase1 * (double)pitchFactor;
                    double readPos2 = (i - phase2) + phase2 * (double)pitchFactor;

                    float s1 = GetSample(data.Samples, readPos1, ch, channels, totalFrames);
                    float s2 = GetSample(data.Samples, readPos2, ch, channels, totalFrames);

                    output[i * channels + ch] = s1 * w1 + s2 * w2;
                }
            }

            Array.Copy(output, data.Samples, data.Samples.Length);

            ReportProgress(1f);
        }

        private static float GetSample(float[] samples, double pos, int channel, int channels, int totalFrames)
        {
            int idx = (int)pos;
            float frac = (float)(pos - idx);

            if (idx < 0) return 0f;
            if (idx >= totalFrames - 1) return idx < totalFrames ? samples[idx * channels + channel] : 0f;

            return samples[idx * channels + channel] * (1f - frac) +
                   samples[(idx + 1) * channels + channel] * frac;
        }
    }
}