using System;
using System.Collections.Generic;
using System.Linq;

namespace TheBirdOfHermes.Audio
{
    public static class AudioLoader
    {
        private static readonly List<IAudioReader> Readers = new List<IAudioReader>
        {
            new WavReader(),
            new OggReader()
        };

        public static void Register(IAudioReader reader) => Readers.Add(reader);

        public static AudioData Load(byte[] bytes)
        {
            foreach (var reader in Readers)
            {
                if (reader.CanRead(bytes))
                    return reader.Read(bytes);
            }
            throw new Exception("Unsupported audio format");
        }

        public static AudioData Load(byte[] bytes, string extension)
        {
            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            foreach (var reader in Readers)
            {
                if (Array.IndexOf(reader.SupportedExtensions, extension) >= 0)
                    return reader.Read(bytes);
            }

            return Load(bytes);
        }

        public static string GetFileFilter()
        {
            var extensions = Readers
                .SelectMany(r => r.SupportedExtensions)
                .Select(e => "*" + e);
            return "Audio Files|" + string.Join(";", extensions.ToArray());
        }
    }
}