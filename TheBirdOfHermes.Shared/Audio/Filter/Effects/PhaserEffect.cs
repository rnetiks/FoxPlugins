using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Phaser effect. Chains first-order allpass filters whose break frequencies
    /// are modulated by an LFO, creating sweeping notches in the frequency spectrum.
    /// </summary>
    public class PhaserEffect : AudioFilterBase
    {
        public override string Name { get; set; } = "Phaser";
        public override string Group { get; set; } = "Effects";

        private string _rateInput = "0.5";
        private string _depthInput = "80";
        private string _stagesInput = "4";
        private string _mixInput = "50";

        private const int MaxStages = 12;

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
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stages:", GUILayout.Width(80));
            _stagesInput = GUILayout.TextField(_stagesInput, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wet Mix:", GUILayout.Width(80));
            _mixInput = GUILayout.TextField(_mixInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Stages: 2-12 (more = deeper effect).\nRate: sweep speed. Depth: sweep range.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_rateInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float rate)) return;
            if (!float.TryParse(_depthInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float depth)) return;
            if (!int.TryParse(_stagesInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stages)) return;
            if (!float.TryParse(_mixInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float mix)) return;

            rate = Mathf.Clamp(rate, 0.01f, 10f);
            depth = Mathf.Clamp(depth, 0f, 100f) / 100f;
            stages = Mathf.Clamp(stages, 2, MaxStages);
            mix = Mathf.Clamp(mix, 0f, 100f) / 100f;

            int sr = data.SampleRate;
            int channels = data.Channels;
            int frames = data.Samples.Length / channels;
            var samples = data.Samples;

            const float minFreq = 200f;
            const float maxFreq = 4000f;

            float phaseInc = rate / sr;

            for (int ch = 0; ch < channels; ch++)
            {
                float[] apState = new float[stages];
                float phase = 0f;

                for (int i = 0; i < frames; i++)
                {
                    if ((i & 4095) == 0)
                        ReportProgress((float)i / frames);
                    
                    int idx = i * channels + ch;
                    float input = samples[idx];

                    float lfo = (float)(0.5 + 0.5 * Math.Sin(2.0 * Math.PI * phase));
                    float sweepFreq = minFreq + (maxFreq - minFreq) * lfo * depth;

                    float tanVal = (float)Math.Tan(Math.PI * sweepFreq / sr);
                    float a = (tanVal - 1f) / (tanVal + 1f);

                    float s = input;
                    for (int st = 0; st < stages; st++)
                    {
                        float yn = a * s + apState[st];
                        apState[st] = s - a * yn;
                        s = yn;
                    }

                    samples[idx] = input * (1f - mix) + s * mix;

                    phase += phaseInc;
                    if (phase >= 1f) phase -= 1f;
                }
            }
            
            ReportProgress(1f);
        }
    }
}