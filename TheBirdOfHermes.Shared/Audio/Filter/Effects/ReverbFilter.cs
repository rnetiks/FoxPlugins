using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    public class ReverbFilter : AudioFilterBase
    {
        public override string Name { get; set; } = "Reverb";
        public override string Group { get; set; } = "Effects";

        private string _roomSizeInput = "0.6";
        private string _dampingInput = "0.4";
        private string _wetMixInput = "0.3";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Room Size:", GUILayout.Width(80));
            _roomSizeInput = GUILayout.TextField(_roomSizeInput, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Damping:", GUILayout.Width(80));
            _dampingInput = GUILayout.TextField(_dampingInput, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wet Mix:", GUILayout.Width(80));
            _wetMixInput = GUILayout.TextField(_wetMixInput, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Size: 0.1-2.0. Damping: 0-1. Wet: 0-1.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_roomSizeInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float roomSize) ||
                !float.TryParse(_dampingInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float damping) ||
                !float.TryParse(_wetMixInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float wetMix))
                return;

            roomSize = Mathf.Clamp(roomSize, 0.1f, 2f);
            damping = Mathf.Clamp01(damping);
            wetMix = Mathf.Clamp01(wetMix);

            int channels = data.Channels;
            int totalFrames = data.Samples.Length / channels;

            for (int ch = 0; ch < channels; ch++)
            {
                ReportProgress((float)ch / channels);

                float[] input = new float[totalFrames];
                for (int i = 0; i < totalFrames; i++)
                    input[i] = data.Samples[i * channels + ch];

                float[] wet = SchroederReverb(input, data.SampleRate, roomSize, damping);

                float dry = 1f - wetMix;
                for (int i = 0; i < totalFrames; i++)
                    data.Samples[i * channels + ch] = input[i] * dry + wet[i] * wetMix;
            }

            ReportProgress(1f);
        }

        private static float[] SchroederReverb(float[] input, int sampleRate, float roomSize, float dampingVal)
        {
            int length = input.Length;
            float scale = sampleRate / 44100f;

            int[] combDelays =
            {
                Mathf.Max(1, (int)(1116 * scale * roomSize)),
                Mathf.Max(1, (int)(1356 * scale * roomSize)),
                Mathf.Max(1, (int)(1491 * scale * roomSize)),
                Mathf.Max(1, (int)(1617 * scale * roomSize))
            };

            int[] apDelays =
            {
                Mathf.Max(1, (int)(556 * scale)),
                Mathf.Max(1, (int)(225 * scale))
            };

            float feedback = 0.7f + 0.15f * roomSize;
            float damp1 = dampingVal;
            float damp2 = 1f - dampingVal;

            float[] combSum = new float[length];

            for (int c = 0; c < 4; c++)
            {
                float[] buffer = new float[combDelays[c]];
                int bufIdx = 0;
                float filterStore = 0f;

                for (int i = 0; i < length; i++)
                {
                    float delayed = buffer[bufIdx];
                    filterStore = delayed * damp2 + filterStore * damp1;
                    buffer[bufIdx] = input[i] + filterStore * feedback;
                    combSum[i] += delayed;
                    bufIdx++;
                    if (bufIdx >= combDelays[c]) bufIdx = 0;
                }
            }

            for (int i = 0; i < length; i++)
                combSum[i] *= 0.25f;

            for (int a = 0; a < 2; a++)
            {
                float[] buffer = new float[apDelays[a]];
                int bufIdx = 0;
                const float apGain = 0.5f;

                for (int i = 0; i < length; i++)
                {
                    float delayed = buffer[bufIdx];
                    float inp = combSum[i];
                    buffer[bufIdx] = inp + delayed * apGain;
                    combSum[i] = delayed - inp * apGain;
                    bufIdx++;
                    if (bufIdx >= apDelays[a]) bufIdx = 0;
                }
            }

            return combSum;
        }
    }
}