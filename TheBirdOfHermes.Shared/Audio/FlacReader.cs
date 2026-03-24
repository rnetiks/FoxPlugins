using System.IO;
using SimpleFlac;

namespace TheBirdOfHermes.Audio
{
    public class FlacReader : IAudioReader
    {
        public string[] SupportedExtensions => new[] { ".flac" };

        public bool CanRead(byte[] headerBytes)
        {
            return headerBytes.Length >= 4
                   && headerBytes[0] == 0x66
                   && headerBytes[1] == 0x4C
                   && headerBytes[2] == 0x61
                   && headerBytes[3] == 0x43;
        }

        public AudioData Read(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var flacDecoder = new FlacDecoder(ms, new FlacDecoder.Options
                   {
                       ConvertOutputToBytes = false,
                       ValidateOutputHash = false
                   }))
            {
                int channels = flacDecoder.ChannelCount;
                int sampleRate = flacDecoder.SampleRate;
                int bitsPerSample = flacDecoder.BitsPerSample;
                float scale = 1f / (1 << (bitsPerSample - 1));

                var sampleList = new System.Collections.Generic.List<float>();

                while (flacDecoder.DecodeFrame())
                {
                    for (int i = 0; i < flacDecoder.BufferSampleCount; i++)
                    for (int c = 0; c < channels; c++)
                        sampleList.Add(flacDecoder.BufferSamples[c][i] * scale);
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