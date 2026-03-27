using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Tremolo effect. Modulates the amplitude of the audio with an LFO,
    /// creating a pulsating volume effect.
    /// </summary>
    public class TremoloEffect : AudioFilterBase
    {
        public override string Name { get; set; } = "Tremolo";
        public override string Group { get; set; } = "Effects";

        private string _rateInput = "5";
        private string _depthInput = "50";

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

            GUILayout.Space(2);
            GUILayout.Label("Rate: pulsation speed (1-20 Hz typical).\nDepth: how much volume varies.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_rateInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float rate)) return;
            if (!float.TryParse(_depthInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float depth)) return;

            rate = Mathf.Clamp(rate, 0.1f, 50f);
            depth = Mathf.Clamp(depth, 0f, 100f) / 100f;

            int sr = data.SampleRate;
            int channels = data.Channels;
            int frames = data.Samples.Length / channels;
            var samples = data.Samples;

            float phaseInc = rate / sr;
            float phase = 0f;

            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames);
                
                float lfo = 1f - depth * 0.5f * (1f - (float)Math.Cos(2.0 * Math.PI * phase));

                for (int ch = 0; ch < channels; ch++)
                    samples[i * channels + ch] *= lfo;

                phase += phaseInc;
                if (phase >= 1f) phase -= 1f;
            }
            
            ReportProgress(1f);
        }
    }
}