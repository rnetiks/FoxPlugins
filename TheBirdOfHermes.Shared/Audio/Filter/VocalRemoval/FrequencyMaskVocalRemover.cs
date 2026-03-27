using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Frequency Mask vocal removal. Uses bandpass filters to isolate the vocal
    /// frequency range, applies center cancellation only to that band, and passes
    /// non-vocal frequencies through untouched. Preserves more of the original mix
    /// than full mid-side removal.
    /// </summary>
    public class FrequencyMaskVocalRemover : AudioFilterBase
    {
        public override string Name { get; set; } = "Frequency Mask";
        public override string Group { get; set; } = "Vocal Removal";

        private string _strengthInput = "100";
        private string _lowFreqInput = "200";
        private string _highFreqInput = "6000";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Strength:", GUILayout.Width(100));
            _strengthInput = GUILayout.TextField(_strengthInput, GUILayout.Width(60));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Low Cutoff:", GUILayout.Width(100));
            _lowFreqInput = GUILayout.TextField(_lowFreqInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("High Cutoff:", GUILayout.Width(100));
            _highFreqInput = GUILayout.TextField(_highFreqInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Removes center content only in the vocal frequency range.\nLow/High define the vocal band. Requires stereo.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (data.Channels < 2) return;

            if (!float.TryParse(_strengthInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float strength)) return;
            if (!float.TryParse(_lowFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float lowFreq)) return;
            if (!float.TryParse(_highFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float highFreq)) return;

            strength = Mathf.Clamp(strength, 0f, 200f) / 100f;
            lowFreq = Mathf.Clamp(lowFreq, 20f, 20000f);
            highFreq = Mathf.Clamp(highFreq, lowFreq + 1f, 20000f);

            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;
            int sr = data.SampleRate;

            var hpL = new Biquad(); hpL.Configure(Biquad.Type.HighPass, sr, lowFreq, 0.707f);
            var hpR = new Biquad(); hpR.Configure(Biquad.Type.HighPass, sr, lowFreq, 0.707f);
            var lpL = new Biquad(); lpL.Configure(Biquad.Type.LowPass, sr, highFreq, 0.707f);
            var lpR = new Biquad(); lpR.Configure(Biquad.Type.LowPass, sr, highFreq, 0.707f);

            var hpL2 = new Biquad(); hpL2.Configure(Biquad.Type.HighPass, sr, lowFreq, 0.707f);
            var hpR2 = new Biquad(); hpR2.Configure(Biquad.Type.HighPass, sr, lowFreq, 0.707f);
            var lpL2 = new Biquad(); lpL2.Configure(Biquad.Type.LowPass, sr, highFreq, 0.707f);
            var lpR2 = new Biquad(); lpR2.Configure(Biquad.Type.LowPass, sr, highFreq, 0.707f);

            float[] vocalBandL = new float[frames];
            float[] vocalBandR = new float[frames];
            float[] cancelledL = new float[frames];
            float[] cancelledR = new float[frames];

            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames * 0.5f);

                int idx = i * channels;
                float left = samples[idx];
                float right = samples[idx + 1];

                float bandL = lpL.Process(hpL.Process(left));
                float bandR = lpR.Process(hpR.Process(right));
                vocalBandL[i] = bandL;
                vocalBandR[i] = bandR;

                float mid = (bandL + bandR) * 0.5f;
                cancelledL[i] = bandL - strength * mid;
                cancelledR[i] = bandR - strength * mid;
            }

            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress(0.5f + (float)i / frames * 0.5f);

                int idx = i * channels;
                float left = samples[idx];
                float right = samples[idx + 1];

                float bandL = lpL2.Process(hpL2.Process(left));
                float bandR = lpR2.Process(hpR2.Process(right));

                samples[idx] = (left - bandL) + cancelledL[i];
                samples[idx + 1] = (right - bandR) + cancelledR[i];
            }

            ReportProgress(1f);
        }
    }
}