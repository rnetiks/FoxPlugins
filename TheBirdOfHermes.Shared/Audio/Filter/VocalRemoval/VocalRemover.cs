using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Mid-Side vocal removal. Decomposes stereo into mid (center) and side (panned)
    /// components, then attenuates mid. Optionally preserves bass frequencies which
    /// are typically mono and would otherwise be lost.
    /// </summary>
    public class VocalRemover : AudioFilterBase
    {
        public override string Name { get; set; } = "Mid-Side";
        public override string Group { get; set; } = "Vocal Removal";

        private string _strengthInput = "100";
        private string _bassPreserveInput = "200";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Strength:", GUILayout.Width(100));
            _strengthInput = GUILayout.TextField(_strengthInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Preserve below:", GUILayout.Width(100));
            _bassPreserveInput = GUILayout.TextField(_bassPreserveInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Removes center-panned audio via stereo difference.\nPreserves bass below cutoff. Requires stereo.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (data.Channels < 2) return;
            if (!float.TryParse(_strengthInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float strength)) return;
            if (!float.TryParse(_bassPreserveInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float bassFreq)) return;

            strength = Mathf.Clamp(strength, 0f, 200f) / 100f;
            bassFreq = Mathf.Clamp(bassFreq, 20f, 2000f);

            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;

            var lpL = new Biquad(); lpL.Configure(Biquad.Type.LowPass, data.SampleRate, bassFreq, 0.707f);
            var lpR = new Biquad(); lpR.Configure(Biquad.Type.LowPass, data.SampleRate, bassFreq, 0.707f);
            var hpL = new Biquad(); hpL.Configure(Biquad.Type.HighPass, data.SampleRate, bassFreq, 0.707f);
            var hpR = new Biquad(); hpR.Configure(Biquad.Type.HighPass, data.SampleRate, bassFreq, 0.707f);

            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames);

                int idx = i * channels;
                float left = samples[idx];
                float right = samples[idx + 1];

                float bassL = lpL.Process(left);
                float bassR = lpR.Process(right);
                float highL = hpL.Process(left);
                float highR = hpR.Process(right);

                float midHigh = (highL + highR) * 0.5f;

                float outHighL = highL - strength * midHigh;
                float outHighR = highR - strength * midHigh;

                samples[idx] = bassL + outHighL;
                samples[idx + 1] = bassR + outHighR;
            }

            ReportProgress(1f);
        }
    }
}