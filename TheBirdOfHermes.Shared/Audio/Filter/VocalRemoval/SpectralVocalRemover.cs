using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// FFT-based spectral vocal removal. For each STFT frame, decomposes left/right
    /// into mid/side in the frequency domain and attenuates mid in the vocal range.
    /// Much higher quality than simple mid-side since it can target specific frequencies.
    /// </summary>
    public class SpectralVocalRemover : AudioFilterBase
    {
        public override string Name { get; set; } = "Spectral";
        public override string Group { get; set; } = "Vocal Removal";

        private string _strengthInput = "100";
        private string _lowFreqInput = "100";
        private string _highFreqInput = "8000";
        private string _fftSizeInput = "4096";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Strength:", GUILayout.Width(100));
            _strengthInput = GUILayout.TextField(_strengthInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Low Freq:", GUILayout.Width(100));
            _lowFreqInput = GUILayout.TextField(_lowFreqInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("High Freq:", GUILayout.Width(100));
            _highFreqInput = GUILayout.TextField(_highFreqInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("FFT Size:", GUILayout.Width(100));
            _fftSizeInput = GUILayout.TextField(_fftSizeInput, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Removes center-panned content in the frequency domain.\nHigher FFT size = better quality but slower. Requires stereo.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (data.Channels < 2) return;

            if (!float.TryParse(_strengthInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float strength)) return;
            if (!float.TryParse(_lowFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float lowFreq)) return;
            if (!float.TryParse(_highFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float highFreq)) return;
            if (!int.TryParse(_fftSizeInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int requestedFft)) return;

            strength = Mathf.Clamp(strength, 0f, 200f) / 100f;
            lowFreq = Mathf.Clamp(lowFreq, 20f, 20000f);
            highFreq = Mathf.Clamp(highFreq, lowFreq, 20000f);

            int fftSize = Mathf.Clamp(FFT.NextPowerOf2(requestedFft), 512, 16384);
            int hopSize = fftSize / 4;

            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;

            float[] left = new float[frames];
            float[] right = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                left[i] = samples[i * channels];
                right[i] = samples[i * channels + 1];
            }

            float[] outL = new float[frames];
            float[] outR = new float[frames];

            float[] window = new float[fftSize];
            for (int i = 0; i < fftSize; i++)
                window[i] = 0.5f * (1f - (float)Math.Cos(2.0 * Math.PI * i / fftSize));

            float[] lR = new float[fftSize], lI = new float[fftSize];
            float[] rR = new float[fftSize], rI = new float[fftSize];

            int lowBin = Math.Max(1, (int)(lowFreq * fftSize / data.SampleRate));
            int highBin = Math.Min(fftSize / 2, (int)(highFreq * fftSize / data.SampleRate));

            int totalHops = (frames - fftSize) / hopSize + 1;
            int hopCount = 0;

            for (int pos = 0; pos <= frames - fftSize; pos += hopSize)
            {
                if ((hopCount & 3) == 0)
                    ReportProgress((float)hopCount / totalHops);
                hopCount++;
                for (int i = 0; i < fftSize; i++)
                {
                    float w = window[i];
                    lR[i] = left[pos + i] * w;
                    rR[i] = right[pos + i] * w;
                    lI[i] = 0f;
                    rI[i] = 0f;
                }

                FFT.Forward(lR, lI, fftSize);
                FFT.Forward(rR, rI, fftSize);

                for (int k = lowBin; k <= highBin; k++)
                {
                    float midR = (lR[k] + rR[k]) * 0.5f;
                    float midI = (lI[k] + rI[k]) * 0.5f;
                    float sideR = (lR[k] - rR[k]) * 0.5f;
                    float sideI = (lI[k] - rI[k]) * 0.5f;

                    float keepMid = 1f - strength;
                    lR[k] = keepMid * midR + sideR;
                    lI[k] = keepMid * midI + sideI;
                    rR[k] = keepMid * midR - sideR;
                    rI[k] = keepMid * midI - sideI;

                    int mirror = fftSize - k;
                    if (mirror != k && mirror < fftSize)
                    {
                        midR = (lR[mirror] + rR[mirror]) * 0.5f;
                        midI = (lI[mirror] + rI[mirror]) * 0.5f;
                        sideR = (lR[mirror] - rR[mirror]) * 0.5f;
                        sideI = (lI[mirror] - rI[mirror]) * 0.5f;

                        lR[mirror] = keepMid * midR + sideR;
                        lI[mirror] = keepMid * midI + sideI;
                        rR[mirror] = keepMid * midR - sideR;
                        rI[mirror] = keepMid * midI - sideI;
                    }
                }

                FFT.Inverse(lR, lI, fftSize);
                FFT.Inverse(rR, rI, fftSize);

                for (int i = 0; i < fftSize; i++)
                {
                    int idx = pos + i;
                    if (idx < frames)
                    {
                        outL[idx] += lR[i] * window[i];
                        outR[idx] += rR[i] * window[i];
                    }
                }
            }

            float normFactor = 1f / (fftSize / (float)hopSize * 0.5f);
            for (int i = 0; i < frames; i++)
            {
                samples[i * channels] = outL[i] * normFactor;
                samples[i * channels + 1] = outR[i] * normFactor;
            }

            ReportProgress(1f);
        }
    }
}