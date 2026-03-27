using System;
using System.IO;
using SimpleFlac;

namespace TheBirdOfHermes.Audio
{
    public class FlacReader : IProgressAudioReader
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
            return ReadWithProgress(bytes, null);
        }

        public AudioData ReadWithProgress(byte[] bytes, Action<float> onProgress)
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

                long totalSamples = (flacDecoder.StreamSampleCount ?? 0) * channels;
                var sampleList = totalSamples > 0
                    ? new System.Collections.Generic.List<float>((int)totalSamples)
                    : new System.Collections.Generic.List<float>();

                long samplesDecoded = 0;

                while (flacDecoder.DecodeFrame())
                {
                    for (int i = 0; i < flacDecoder.BufferSampleCount; i++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            sampleList.Add(flacDecoder.BufferSamples[c][i] * scale);
                            samplesDecoded++;
                        }
                    }

                    if (totalSamples > 0)
                        onProgress?.Invoke((float)samplesDecoded / totalSamples);
                }

                onProgress?.Invoke(1f);

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