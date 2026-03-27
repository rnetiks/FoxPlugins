using System;
using System.Globalization;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Simple gain adjustment in decibels.
    /// </summary>
    public class Amplifier : AudioFilterBase
    {
        public override string Name { get; set; } = "Amplify";
        public override string Group { get; set; } = "Dynamics";

        private string _gainInput = "6";

        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Gain:", GUILayout.Width(80));
            _gainInput = GUILayout.TextField(_gainInput, GUILayout.Width(60));
            GUILayout.Label("dB");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("Positive = louder, negative = quieter.\n6 dB ≈ double perceived volume.", WindowStyles.HintLabel);
        }

        public override void Process(AudioData data)
        {
            if (!float.TryParse(_gainInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float gainDb)) return;
            gainDb = Mathf.Clamp(gainDb, -60f, 60f);
            if (Mathf.Abs(gainDb) < 0.01f) return;

            float gain = (float)Math.Pow(10.0, gainDb / 20.0);

            var samples = data.Samples;
            for (int i = 0; i < samples.Length; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / samples.Length);
                
                samples[i] *= gain;
            }
            
            ReportProgress(1f);
        }
    }
}