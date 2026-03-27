using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    public class SpeedChanger : AudioFilterBase
    {
        public override string Name { get; set; } = "Speed Change";
        public override string Group { get; set; } = "Pitch & Speed";

        private string _speedInput = "100";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", GUILayout.Width(80));
            _speedInput = GUILayout.TextField(_speedInput, GUILayout.Width(80));
            GUILayout.Label("%");
            GUILayout.EndHorizontal();
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_speedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float speedFactor))
                return;
            speedFactor /= 100f;
            if (speedFactor < 0.01f || Mathf.Abs(speedFactor - 1f) < 0.001f) return;

            int channels = data.Channels;
            int oldFrames = data.Samples.Length / channels;
            int newFrames = Mathf.Max(1, (int)(oldFrames / speedFactor));
            float[] output = new float[newFrames * channels];

            for (int i = 0; i < newFrames; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress(i / (float)newFrames);
                
                double srcPos = i * (double)speedFactor;
                int idx = (int)srcPos;
                float frac = (float)(srcPos - idx);

                int next = System.Math.Min(idx + 1, oldFrames - 1);
                idx = System.Math.Min(idx, oldFrames - 1);

                for (int ch = 0; ch < channels; ch++)
                {
                    output[i * channels + ch] =
                        data.Samples[idx * channels + ch] * (1f - frac) +
                        data.Samples[next * channels + ch] * frac;
                }
            }

            ReportProgress(1f);

            data.Samples = output;
        }
    }
}