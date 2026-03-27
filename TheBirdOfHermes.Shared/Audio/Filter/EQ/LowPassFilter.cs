using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Low-pass filter. Removes frequencies above the cutoff.
    /// Uses cascaded 2nd-order biquad sections for steeper rolloff.
    /// </summary>
    public class LowPassFilter : AudioFilterBase
    {
        public override string Name { get; set; } = "Low Pass";
        public override string Group { get; set; } = "EQ";

        private string _cutoffInput = "5000";
        private string _orderInput = "2";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cutoff:", GUILayout.Width(80));
            _cutoffInput = GUILayout.TextField(_cutoffInput, GUILayout.Width(60));
            GUILayout.Label("Hz");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Order:", GUILayout.Width(80));
            _orderInput = GUILayout.TextField(_orderInput, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Removes high frequencies above cutoff.\nOrder 1-4 (higher = steeper rolloff, 6dB per order).", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_cutoffInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float cutoff)) return;
            if (!int.TryParse(_orderInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int order)) return;

            cutoff = Mathf.Clamp(cutoff, 20f, data.SampleRate / 2f - 100f);
            order = Mathf.Clamp(order, 1, 4);

            int channels = data.Channels;
            int frames = data.Samples.Length / channels;

            var filters = new Biquad[channels * order];
            for (int ch = 0; ch < channels; ch++)
            {
                for (int o = 0; o < order; o++)
                {
                    var f = new Biquad();
                    f.Configure(Biquad.Type.LowPass, data.SampleRate, cutoff, 0.707f);
                    filters[ch * order + o] = f;
                }
            }

            var samples = data.Samples;
            for (int i = 0; i < frames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames);
                
                for (int ch = 0; ch < channels; ch++)
                {
                    int idx = i * channels + ch;
                    float s = samples[idx];
                    for (int o = 0; o < order; o++)
                        s = filters[ch * order + o].Process(s);
                    samples[idx] = s;
                }
            }
            
            ReportProgress(1f);
        }
    }
}