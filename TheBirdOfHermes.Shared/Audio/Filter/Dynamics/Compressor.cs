using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Dynamic range compressor. Reduces the volume of loud sounds above
    /// the threshold by the given ratio, with configurable attack and release.
    /// Includes optional makeup gain to restore perceived loudness.
    /// </summary>
    public class Compressor : AudioFilterBase
    {
        public override string Name { get; set; } = "Compressor";
        public override string Group { get; set; } = "Dynamics";

        private string _thresholdInput = "-20";
        private string _ratioInput = "4";
        private string _attackInput = "10";
        private string _releaseInput = "100";
        private string _makeupInput = "0";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Threshold:", GUILayout.Width(80));
            _thresholdInput = GUILayout.TextField(_thresholdInput, GUILayout.Width(60));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ratio:", GUILayout.Width(80));
            _ratioInput = GUILayout.TextField(_ratioInput, GUILayout.Width(60));
            GUILayout.Label(":1");
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

            GUILayout.BeginHorizontal();
            GUILayout.Label("Makeup:", GUILayout.Width(80));
            _makeupInput = GUILayout.TextField(_makeupInput, GUILayout.Width(60));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Threshold: level where compression starts.\nRatio: compression amount (4:1 = moderate).", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_thresholdInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float threshDb)) return;
            if (!float.TryParse(_ratioInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float ratio)) return;
            if (!float.TryParse(_attackInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float attackMs)) return;
            if (!float.TryParse(_releaseInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float releaseMs)) return;
            if (!float.TryParse(_makeupInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float makeupDb)) return;

            threshDb = Mathf.Clamp(threshDb, -60f, 0f);
            ratio = Mathf.Clamp(ratio, 1f, 100f);
            attackMs = Mathf.Clamp(attackMs, 0.1f, 500f);
            releaseMs = Mathf.Clamp(releaseMs, 1f, 5000f);
            makeupDb = Mathf.Clamp(makeupDb, -20f, 40f);

            float threshold = (float)Math.Pow(10.0, threshDb / 20.0);
            float makeupGain = (float)Math.Pow(10.0, makeupDb / 20.0);
            float attackCoeff = (float)Math.Exp(-1.0 / (attackMs * 0.001 * data.SampleRate));
            float releaseCoeff = (float)Math.Exp(-1.0 / (releaseMs * 0.001 * data.SampleRate));

            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;

            float envelope = 0f;

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

                if (peak > envelope)
                    envelope = attackCoeff * envelope + (1f - attackCoeff) * peak;
                else
                    envelope = releaseCoeff * envelope + (1f - releaseCoeff) * peak;

                float gain = 1f;
                if (envelope > threshold && envelope > 1e-10f)
                    gain = (float)Math.Pow(envelope / threshold, 1f / ratio - 1f);

                gain *= makeupGain;

                for (int ch = 0; ch < channels; ch++)
                    samples[i * channels + ch] *= gain;
            }

            ReportProgress(1f);
        }
    }
}