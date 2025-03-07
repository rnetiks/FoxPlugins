using System;

namespace ColliderSound.KK
{
    public class Wav
    {
        public float[] LeftChannel;
        public int Channels;
        public int SampleCount;
        public int Frequency;

        private const int I = 44;

        public Wav(byte[] wav)
        {
            Channels = BitConverter.ToInt16(wav, 22);
            Frequency = BitConverter.ToInt32(wav, 24);
            var bitsPerSample = BitConverter.ToInt16(wav, 34);
            if (bitsPerSample != 16)
            {
                throw new Exception("Invalid WAV format");
            }

            SampleCount = (wav.Length - I) / (2 * Channels);

            LeftChannel = new float[SampleCount * Channels];

            int index = I;
            for (int i = 0; i < SampleCount * Channels; i++)
            {
                LeftChannel[i] = BitConverter.ToInt16(wav, index) / 32768f;
                index += 2;
            }

            LeftChannel = FadeIn(LeftChannel, Frequency, Entry.fadeInTime.Value);
        }

        private float[] FadeIn(float[] audioData, int sampleRate, float fadeDuration)
        {
            int fadeSamples = (int)(sampleRate * fadeDuration);
            for (int i = 0; i < fadeSamples && i < audioData.Length; i++)
            {
                float multiplier = i / (float)fadeSamples;
                audioData[i] *= multiplier;
            }

            return audioData;
        }
    }
}