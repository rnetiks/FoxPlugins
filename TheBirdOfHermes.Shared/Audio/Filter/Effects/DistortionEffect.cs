using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Distortion effect using tanh soft-clipping waveshaper.
    /// Drive controls the input gain before clipping. Higher drive = more harmonics.
    /// Includes a post-filter to tame harsh high frequencies.
    /// </summary>
    public class DistortionEffect : AudioFilterBase
    {
        public override string Name { get; set; } = "Distortion";
        public override string Group { get; set; } = "Effects";

        private string _driveInput = "10";
        private string _mixInput = "80";
        private string _toneInput = "6000";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Drive:", GUILayout.Width(80));
            _driveInput = GUILayout.TextField(_driveInput, GUILayout.Width(60));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tone:", GUILayout.Width(80));
            _toneInput = GUILayout.TextField(_toneInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wet Mix:", GUILayout.Width(80));
            _mixInput = GUILayout.TextField(_mixInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Drive: distortion amount. Tone: post-filter cutoff.\nUses soft clipping (tanh waveshaper).", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_driveInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float driveDb)) return;
            if (!float.TryParse(_mixInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float mix)) return;
            if (!float.TryParse(_toneInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float tone)) return;

            driveDb = Mathf.Clamp(driveDb, 0f, 40f);
            mix = Mathf.Clamp(mix, 0f, 100f) / 100f;
            tone = Mathf.Clamp(tone, 500f, 20000f);

            float driveGain = (float)Math.Pow(10.0, driveDb / 20.0);

            int channels = data.Channels;
            int frames = data.Samples.Length / channels;
            var samples = data.Samples;

            var toneFilters = new Biquad[channels];
            for (int ch = 0; ch < channels; ch++)
            {
                toneFilters[ch] = new Biquad();
                toneFilters[ch].Configure(Biquad.Type.LowPass, data.SampleRate, tone, 0.707f);
            }

            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames);
                
                for (int ch = 0; ch < channels; ch++)
                {
                    int idx = i * channels + ch;
                    float input = samples[idx];

                    float driven = input * driveGain;
                    float clipped = (float)Math.Tanh(driven);

                    clipped = toneFilters[ch].Process(clipped);

                    samples[idx] = input * (1f - mix) + clipped * mix;
                }
            }
            
            ReportProgress(1f);
        }
    }
}