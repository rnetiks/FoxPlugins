using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Chorus effect. Uses a modulated delay line to create a thicker,
    /// shimmering sound by mixing the original with slightly detuned copies.
    /// </summary>
    public class ChorusEffect : AudioFilterBase
    {
        public override string Name { get; set; } = "Chorus";
        public override string Group { get; set; } = "Effects";

        private string _rateInput = "1.5";
        private string _depthInput = "5";
        private string _mixInput = "50";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rate:", GUILayout.Width(80));
            _rateInput = GUILayout.TextField(_rateInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Depth:", GUILayout.Width(80));
            _depthInput = GUILayout.TextField(_depthInput, GUILayout.Width(60));
            GUILayout.Label("ms");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wet Mix:", GUILayout.Width(80));
            _mixInput = GUILayout.TextField(_mixInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Rate: LFO speed. Depth: modulation range.\nCreates a richer, wider sound.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_rateInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float rate)) return;
            if (!float.TryParse(_depthInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float depthMs)) return;
            if (!float.TryParse(_mixInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float mix)) return;

            rate = Mathf.Clamp(rate, 0.05f, 20f);
            depthMs = Mathf.Clamp(depthMs, 0.1f, 30f);
            mix = Mathf.Clamp(mix, 0f, 100f) / 100f;

            int sr = data.SampleRate;
            int channels = data.Channels;
            int frames = data.Samples.Length / channels;

            int baseDelay = (int)(20f * 0.001f * sr);
            int depthSamples = (int)(depthMs * 0.001f * sr);
            int bufferLen = baseDelay + depthSamples + 4;

            float phaseInc = rate / sr;
            var samples = data.Samples;

            for (int ch = 0; ch < channels; ch++)
            {
                float[] buf = new float[bufferLen];
                int writePos = 0;
                float phase = 0f;

                for (int i = 0; i < frames; i++)
                {
                    if ((i & 4095) == 0)
                        ReportProgress(((float)ch / channels) + ((float)i / frames / channels));

                    int idx = i * channels + ch;
                    float input = samples[idx];

                    buf[writePos] = input;

                    float lfo = (float)Math.Sin(2.0 * Math.PI * phase);
                    float readDelay = baseDelay + lfo * depthSamples;

                    float readPosF = writePos - readDelay;
                    while (readPosF < 0) readPosF += bufferLen;

                    int readIdx = (int)readPosF;
                    float frac = readPosF - readIdx;
                    readIdx %= bufferLen;
                    int nextIdx = (readIdx + 1) % bufferLen;

                    float delayed = buf[readIdx] * (1f - frac) + buf[nextIdx] * frac;

                    samples[idx] = input * (1f - mix) + delayed * mix;

                    phase += phaseInc;
                    if (phase >= 1f) phase -= 1f;
                    writePos = (writePos + 1) % bufferLen;
                }
            }

            ReportProgress(1f);
        }
    }
}