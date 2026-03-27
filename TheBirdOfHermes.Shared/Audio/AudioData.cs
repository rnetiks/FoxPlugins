using UnityEngine;

namespace TheBirdOfHermes.Audio
{
    public class AudioData
    {
        /// <summary>
        /// Represents an array of sample data for audio processing. Each element in the array corresponds to an audio sample,
        /// with the number of samples depending on the audio's duration, sample rate, and number of channels.
        /// </summary>
        public float[] Samples;
        /// <summary>
        /// Represents the number of audio samples processed or played per second.
        /// Determines the precision and quality of the audio data.
        /// </summary>
        public int SampleRate;
        /// <summary>
        /// Specifies the number of audio channels present in the audio data. A value of 1 indicates mono audio, while 2 indicates stereo.
        /// This value is used in conjunction with the sample rate and duration to determine the structure and processing of the audio data.
        /// </summary>
        public int Channels;
        /// <summary>
        /// Represents the total duration of the audio data in seconds.
        /// Calculated based on the length of the sample array, sample rate, and number of channels.
        /// </summary>
        public float Duration => Samples.Length / (float)(SampleRate * Channels);

        public byte[] EncodeWav()
        {
            int sampleCount = Samples.Length;
            int bytesPerSample = 2;
            int blockAlign = Channels * bytesPerSample;
            int byteRate = SampleRate * blockAlign;
            int dataSize = sampleCount * bytesPerSample;
            int fileSize = 44 + dataSize;

            byte[] wav = new byte[fileSize];

            wav[0] = (byte)'R'; wav[1] = (byte)'I'; wav[2] = (byte)'F'; wav[3] = (byte)'F';
            WriteInt(wav, 4, fileSize - 8);
            wav[8] = (byte)'W'; wav[9] = (byte)'A'; wav[10] = (byte)'V'; wav[11] = (byte)'E';

            wav[12] = (byte)'f'; wav[13] = (byte)'m'; wav[14] = (byte)'t'; wav[15] = (byte)' ';
            WriteInt(wav, 16, 16);
            WriteShort(wav, 20, 1);
            WriteShort(wav, 22, (short)Channels);
            WriteInt(wav, 24, SampleRate);
            WriteInt(wav, 28, byteRate);
            WriteShort(wav, 32, (short)blockAlign);
            WriteShort(wav, 34, (short)(bytesPerSample * 8));

            wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';
            WriteInt(wav, 40, dataSize);

            int offset = 44;
            for (int i = 0; i < sampleCount; i++)
            {
                short s = (short)(Mathf.Clamp(Samples[i], -1f, 1f) * 32767f);
                wav[offset++] = (byte)(s & 0xFF);
                wav[offset++] = (byte)((s >> 8) & 0xFF);
            }

            return wav;
        }

        private static void WriteInt(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteShort(byte[] buffer, int offset, short value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }
    }
}