using System;

namespace TheBirdOfHermes.Audio.Filter
{
    /// <summary>
    /// Reverses the audio. Preserves channel interleaving. No parameters needed.
    /// </summary>
    public class ReverseFilter : AudioFilterBase
    {
        public override string Name { get; set; } = "Reverse";
        public override string Group { get; set; } = "Utility";

        public override void Process(AudioData data)
        {
            var samples = data.Samples;
            int channels = data.Channels;
            int frames = samples.Length / channels;

            for (int i = 0; i < frames / 2; i++)
            {
                if ((i & 4095) == 0)
                    ReportProgress((float)i / frames);
                
                int j = frames - 1 - i;
                for (int ch = 0; ch < channels; ch++)
                {
                    int a = i * channels + ch;
                    int b = j * channels + ch;
                    float tmp = samples[a];
                    samples[a] = samples[b];
                    samples[b] = tmp;
                }
            }
            
            ReportProgress(1f);
        }
    }
}