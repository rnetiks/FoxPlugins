using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// 3-band parametric equalizer using low shelf, peaking mid, and high shelf filters.
    /// </summary>
    public class Equalizer : AudioFilterBase
    {
        public override string Name { get; set; } = "3-Band EQ";
        public override string Group { get; set; } = "EQ";

        private string _bassFreqInput = "100";
        private string _bassGainInput = "0";
        private string _midFreqInput = "1000";
        private string _midGainInput = "0";
        private string _midQInput = "1";
        private string _trebleFreqInput = "8000";
        private string _trebleGainInput = "0";

        public override void OnDraw()
        {
            GUILayout.Label("Bass (Low Shelf)", WindowStyles.LabelBold);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Freq:", GUILayout.Width(40));
            _bassFreqInput = GUILayout.TextField(_bassFreqInput, GUILayout.Width(50));
            GUILayout.Label("Hz  Gain:", GUILayout.Width(60));
            _bassGainInput = GUILayout.TextField(_bassGainInput, GUILayout.Width(40));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Mid (Peaking)", WindowStyles.LabelBold);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Freq:", GUILayout.Width(40));
            _midFreqInput = GUILayout.TextField(_midFreqInput, GUILayout.Width(50));
            GUILayout.Label("Hz  Gain:", GUILayout.Width(60));
            _midGainInput = GUILayout.TextField(_midGainInput, GUILayout.Width(40));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Q:", GUILayout.Width(40));
            _midQInput = GUILayout.TextField(_midQInput, GUILayout.Width(50));
            GUILayout.Label("(0.1 = wide, 10 = narrow)");
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Treble (High Shelf)", WindowStyles.LabelBold);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Freq:", GUILayout.Width(40));
            _trebleFreqInput = GUILayout.TextField(_trebleFreqInput, GUILayout.Width(50));
            GUILayout.Label("Hz  Gain:", GUILayout.Width(60));
            _trebleGainInput = GUILayout.TextField(_trebleGainInput, GUILayout.Width(40));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Gain: -24 to +24 dB per band.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_bassFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float bassFreq)) return;
            if (!float.TryParse(_bassGainInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float bassGain)) return;
            if (!float.TryParse(_midFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float midFreq)) return;
            if (!float.TryParse(_midGainInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float midGain)) return;
            if (!float.TryParse(_midQInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float midQ)) return;
            if (!float.TryParse(_trebleFreqInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float trebleFreq)) return;
            if (!float.TryParse(_trebleGainInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float trebleGain)) return;

            bassFreq = Mathf.Clamp(bassFreq, 20f, 1000f);
            bassGain = Mathf.Clamp(bassGain, -24f, 24f);
            midFreq = Mathf.Clamp(midFreq, 100f, 10000f);
            midGain = Mathf.Clamp(midGain, -24f, 24f);
            midQ = Mathf.Clamp(midQ, 0.1f, 10f);
            trebleFreq = Mathf.Clamp(trebleFreq, 1000f, 20000f);
            trebleGain = Mathf.Clamp(trebleGain, -24f, 24f);

            if (Mathf.Abs(bassGain) < 0.1f && Mathf.Abs(midGain) < 0.1f && Mathf.Abs(trebleGain) < 0.1f) return;

            int channels = data.Channels;
            int frames = data.Samples.Length / channels;
            int sr = data.SampleRate;

            var filters = new Biquad[channels * 3];
            for (int ch = 0; ch < channels; ch++)
            {
                var bass = new Biquad();
                bass.Configure(Biquad.Type.LowShelf, sr, bassFreq, 0.707f, bassGain);
                filters[ch * 3] = bass;

                var mid = new Biquad();
                mid.Configure(Biquad.Type.PeakingEQ, sr, midFreq, midQ, midGain);
                filters[ch * 3 + 1] = mid;

                var treble = new Biquad();
                treble.Configure(Biquad.Type.HighShelf, sr, trebleFreq, 0.707f, trebleGain);
                filters[ch * 3 + 2] = treble;
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
                    s = filters[ch * 3].Process(s);
                    s = filters[ch * 3 + 1].Process(s);
                    s = filters[ch * 3 + 2].Process(s);
                    samples[idx] = s;
                }
            }
            
            ReportProgress(1f);
        }
    }
}