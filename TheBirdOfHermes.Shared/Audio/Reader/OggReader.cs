using System;
using System.IO;
using NVorbis;

namespace TheBirdOfHermes.Audio
{
    public class OggReader : IProgressAudioReader
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
            return ReadWithProgress(bytes, null);
        }

        public AudioData ReadWithProgress(byte[] bytes, Action<float> onProgress)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new VorbisReader(ms, false))
            {
                int channels = reader.Channels;
                int sampleRate = reader.SampleRate;
                long totalSamples = reader.TotalSamples * channels;

                var sampleList = totalSamples > 0
                    ? new System.Collections.Generic.List<float>((int)totalSamples)
                    : new System.Collections.Generic.List<float>();

                float[] buffer = new float[sampleRate * channels];
                long samplesDecoded = 0;

                int samplesRead;
                while ((samplesRead = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < samplesRead; i++)
                        sampleList.Add(buffer[i]);

                    samplesDecoded += samplesRead;
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