using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Peak normalizer. Scales the entire audio so the loudest sample
    /// reaches the target peak level. Preserves dynamics, only adjusts overall volume.
    /// </summary>
    public class Normalizer : AudioFilterBase
    {
        public override string Name { get; set; } = "Normalize";
        public override string Group { get; set; } = "Dynamics";

        private string _targetInput = "0";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Peak:", GUILayout.Width(80));
            _targetInput = GUILayout.TextField(_targetInput, GUILayout.Width(60));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("0 dB = maximum without clipping.\n-3 dB = slight headroom.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_targetInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float targetDb)) return;
            targetDb = Mathf.Clamp(targetDb, -40f, 0f);

            float targetLinear = (float)Math.Pow(10.0, targetDb / 20.0);

            float peak = 0f;
            var samples = data.Samples;
            for (int i = 0; i < samples.Length; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / samples.Length * 0.5f);
                
                float abs = Math.Abs(samples[i]);
                if (abs > peak) peak = abs;
            }

            if (peak < 1e-10f) return;

            float gain = targetLinear / peak;

            for (int i = 0; i < samples.Length; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / samples.Length * 0.5f + 0.5f);
                
                samples[i] *= gain;
            }
            
            ReportProgress(1f);
        }
    }
}