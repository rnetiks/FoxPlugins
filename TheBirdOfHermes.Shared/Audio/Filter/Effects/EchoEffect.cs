using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Echo / delay effect. Creates repeating echoes with configurable
    /// delay time and decay. Supports multiple echo taps via feedback.
    /// </summary>
    public class EchoEffect : AudioFilterBase
    {
        public override string Name { get; set; } = "Echo";
        public override string Group { get; set; } = "Effects";

        private string _delayInput = "250";
        private string _decayInput = "50";
        private string _mixInput = "40";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Delay:", GUILayout.Width(80));
            _delayInput = GUILayout.TextField(_delayInput, GUILayout.Width(60));
            GUILayout.Label("ms");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Decay:", GUILayout.Width(80));
            _decayInput = GUILayout.TextField(_decayInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wet Mix:", GUILayout.Width(80));
            _mixInput = GUILayout.TextField(_mixInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Delay: time between echoes.\nDecay: volume reduction per echo.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_delayInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float delayMs)) return;
            if (!float.TryParse(_decayInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float decay)) return;
            if (!float.TryParse(_mixInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float mix)) return;

            delayMs = Mathf.Clamp(delayMs, 1f, 5000f);
            decay = Mathf.Clamp(decay, 0f, 95f) / 100f;
            mix = Mathf.Clamp(mix, 0f, 100f) / 100f;

            int delaySamples = (int)(delayMs * 0.001f * data.SampleRate);
            if (delaySamples < 1) return;

            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;

            for (int ch = 0; ch < channels; ch++)
            {
                float[] buffer = new float[delaySamples];
                int writePos = 0;

                for (int i = 0; i < frames; i++)
                {
                    if ((i & 4095) == 0)
                        ReportProgress(((float)ch / channels) + ((float)i / frames / channels));

                    int idx = i * channels + ch;
                    float input = samples[idx];
                    float delayed = buffer[writePos];

                    buffer[writePos] = input + delayed * decay;
                    writePos = (writePos + 1) % delaySamples;

                    samples[idx] = input * (1f - mix) + delayed * mix;
                }
            }

            ReportProgress(1f);
        }
    }
}