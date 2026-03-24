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
            new OggReader(),
            new MP3Reader(),
            new FlacReader()
        };

        /// <summary>
        /// Registers a new audio reader to be used for loading audio data.
        /// </summary>
        /// <param name="reader">The audio reader to register for use with audio file loading.</param>
        public static void Register(IAudioReader reader) => Readers.Add(reader);

        public static bool Unregister(IAudioReader reader) => Readers.Remove(reader);

        /// <summary>
        /// Loads audio data from a byte array by attempting to use all registered audio readers.
        /// </summary>
        /// <param name="bytes">The byte array containing the raw audio file data.</param>
        /// <returns>
        /// An <see cref="AudioData"/> object containing the loaded audio samples,
        /// sample rate, and channel data.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when none of the registered audio readers support the provided audio format.
        /// </exception>
        public static AudioData Load(byte[] bytes)
        {
            foreach (var reader in Readers)
            {
                if (reader.CanRead(bytes))
                    return reader.Read(bytes);
            }
            throw new Exception("Unsupported audio format");
        }

        /// <summary>
        /// Loads audio data from a byte array using the specified file extension
        /// to determine the appropriate audio reader. If no matching reader for the
        /// extension is found, attempts to load the data with all registered readers.
        /// </summary>
        /// <param name="bytes">The byte array containing the raw audio file data.</param>
        /// <param name="extension">The file extension (e.g., ".wav", ".ogg") used
        /// to identify the correct audio reader.</param>
        /// <returns>
        /// An <see cref="AudioData"/> object containing the loaded audio samples,
        /// sample rate, and channel data.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the audio format is unsupported by all registered audio readers.
        /// </exception>
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

        /// <summary>
        /// Generates a file filter string for supported audio file extensions.
        /// The file filter string is used in file dialogs to show and filter audio files
        /// based on the extensions supported by registered audio readers.
        /// </summary>
        /// <returns>
        /// A file filter string formatted as "Audio Files|*.ext1;*.ext2",
        /// where ext1, ext2, etc., are the supported audio file extensions for registered readers.
        /// </returns>
        public static string GetFileFilter()
        {
            var extensions = Readers
                .SelectMany(r => r.SupportedExtensions)
                .Select(e => "*" + e);
            return "Audio Files|" + string.Join(";", extensions.ToArray());
        }
    }
}