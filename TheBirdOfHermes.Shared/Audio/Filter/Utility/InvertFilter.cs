namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Inverts the audio waveform (flips polarity). No parameters needed.
    /// </summary>
    public class InvertFilter : AudioFilterBase
    {
        public override string Name { get; set; } = "Invert";
        public override string Group { get; set; } = "Utility";

        public override void Process(AudioData data)
        {
            var samples = data.Samples;
            for (int i = 0; i < samples.Length; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / samples.Length);
                
                samples[i] = -samples[i];
            }
            ReportProgress(1f);
        }
    }
}