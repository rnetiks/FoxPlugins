using System.IO;
using NVorbis;

namespace TheBirdOfHermes.Audio
{
    public class OggReader : IAudioReader
    {
        public string[] SupportedExtensions => new[] { ".ogg" };

        public bool CanRead(byte[] headerBytes)
        {
            if (headerBytes == null || headerBytes.Length < 4)
                return false;
            return headerBytes[0] == 0x4F && headerBytes[1] == 0x67
                && headerBytes[2] == 0x67 && headerBytes[3] == 0x53;
        }

        public AudioData Read(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new VorbisReader(ms, false))
            {
                int channels = reader.Channels;
                int sampleRate = reader.SampleRate;

                var sampleList = new System.Collections.Generic.List<float>();
                float[] buffer = new float[sampleRate * channels];

                int samplesRead;
                while ((samplesRead = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < samplesRead; i++)
                    {
                        sampleList.Add(buffer[i]);
                    }
                }

                return new AudioData
                {
                    Samples = sampleList.ToArray(),
                    SampleRate = sampleRate,
                    Channels = channels
                };
            }
        }
    }
}