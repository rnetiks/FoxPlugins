namespace TheBirdOfHermes.Audio
{
    public class AudioData
    {
        public float[] Samples;
        public int SampleRate;
        public int Channels;
        public float Duration => Samples.Length / (float)(SampleRate * Channels);
    }
}