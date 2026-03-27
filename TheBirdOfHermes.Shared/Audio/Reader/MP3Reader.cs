using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLayer;

namespace TheBirdOfHermes.Audio
{
    public class MP3Reader : IProgressAudioReader
    {
        public string[] SupportedExtensions => new [] { ".mp3", ".mp2" };

        public bool CanRead(byte[] headerBytes)
        {
            if (headerBytes.Length < 2)
                return false;

            if (headerBytes.Length >= 3 &&
                headerBytes[0] == 0x49 &&
                headerBytes[1] == 0x44 &&
                headerBytes[2] == 0x33)
                return true;

            if (headerBytes[0] != 0xFF || (headerBytes[1] & 0xE0) != 0xE0)
                return false;

            int version = (headerBytes[1] >> 3) & 0x03;
            if (version == 0x01)
                return false;

            int layer = (headerBytes[1] >> 1) & 0x03;
            if (layer == 0x00)
                return false;

            if (headerBytes.Length >= 4)
            {
                int bitrate    = (headerBytes[2] >> 4) & 0x0F;
                int sampleRate = (headerBytes[2] >> 2) & 0x03;
                if (bitrate == 0x0F || sampleRate == 0x03)
                    return false;
            }

            return true;
        }

        public AudioData Read(byte[] bytes)
        {
            return ReadWithProgress(bytes, null);
        }

        public AudioData ReadWithProgress(byte[] bytes, Action<float> onProgress)
        {
            var ms = new MemoryStream(bytes);
            var mpegFile = new MpegFile(ms);

            long totalSamplesEstimate = 0;
            if (mpegFile.Duration.TotalSeconds > 0)
                totalSamplesEstimate = (long)(mpegFile.Duration.TotalSeconds * mpegFile.SampleRate * mpegFile.Channels);

            var samples = totalSamplesEstimate > 0
                ? new List<float>((int)totalSamplesEstimate)
                : new List<float>();

            var buffer = new float[4096];
            int read;

            while ((read = mpegFile.ReadSamples(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    samples.Add(buffer[i]);

                if (totalSamplesEstimate > 0)
                    onProgress?.Invoke(Math.Min(1f, (float)samples.Count / totalSamplesEstimate));
            }

            onProgress?.Invoke(1f);

            return new AudioData
            {
                Channels = mpegFile.Channels,
                SampleRate = mpegFile.SampleRate,
                Samples = samples.ToArray()
            };
        }
    }
}