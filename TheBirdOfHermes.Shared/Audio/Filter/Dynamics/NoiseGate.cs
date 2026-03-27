using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Noise gate. Silences audio below the threshold level.
    /// Uses attack/release smoothing to avoid clicks.
    /// </summary>
    public class NoiseGate : AudioFilterBase
    {
        public override string Name { get; set; } = "Noise Gate";
        public override string Group { get; set; } = "Dynamics";

        private string _thresholdInput = "-40";
        private string _attackInput = "1";
        private string _releaseInput = "50";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Threshold:", GUILayout.Width(80));
            _thresholdInput = GUILayout.TextField(_thresholdInput, GUILayout.Width(60));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Attack:", GUILayout.Width(80));
            _attackInput = GUILayout.TextField(_attackInput, GUILayout.Width(60));
            GUILayout.Label("ms");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Release:", GUILayout.Width(80));
            _releaseInput = GUILayout.TextField(_releaseInput, GUILayout.Width(60));
            GUILayout.Label("ms");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Silences audio below the threshold.\nUseful for removing background noise between sounds.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_thresholdInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float threshDb)) return;
            if (!float.TryParse(_attackInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float attackMs)) return;
            if (!float.TryParse(_releaseInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float releaseMs)) return;

            threshDb = Mathf.Clamp(threshDb, -80f, 0f);
            attackMs = Mathf.Clamp(attackMs, 0.1f, 200f);
            releaseMs = Mathf.Clamp(releaseMs, 1f, 2000f);

            float threshold = (float)Math.Pow(10.0, threshDb / 20.0);
            float attackCoeff = (float)Math.Exp(-1.0 / (attackMs * 0.001 * data.SampleRate));
            float releaseCoeff = (float)Math.Exp(-1.0 / (releaseMs * 0.001 * data.SampleRate));

            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;

            float envelope = 0f;
            float gateGain = 0f;

            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames);
                
                float peak = 0f;
                for (int ch = 0; ch < channels; ch++)
                {
                    float abs = Math.Abs(samples[i * channels + ch]);
                    if (abs > peak) peak = abs;
                }

                float envCoeff = peak > envelope ? 0.999f : releaseCoeff;
                envelope = envCoeff * envelope + (1f - envCoeff) * peak;

                float targetGain = envelope > threshold ? 1f : 0f;

                if (targetGain > gateGain)
                    gateGain = attackCoeff * gateGain + (1f - attackCoeff) * targetGain;
                else
                    gateGain = releaseCoeff * gateGain + (1f - releaseCoeff) * targetGain;

                for (int ch = 0; ch < channels; ch++)
                    samples[i * channels + ch] *= gateGain;
            }
            
            ReportProgress(1f);
        }
    }
}