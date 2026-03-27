using System;

namespace TheBirdOfHermes.Audio
{
    public interface IAudioReader
    {
        /// Gets a list of file extensions supported by the audio reader.
        string[] SupportedExtensions { get; }
        /// Determines if the specified byte array matches the audio format that the reader can handle.
        bool CanRead(byte[] headerBytes);
        /// Reads audio data from the provided byte array and processes it into an AudioData instance.
        AudioData Read(byte[] bytes);
    }

    /// <summary>
    /// Extended reader interface that reports decoding progress (0..1).
    /// Readers that implement this allow the streaming system to show progress bars.
    /// </summary>
    public interface IProgressAudioReader : IAudioReader
    {
        /// <summary>
        /// Reads audio with progress reporting. Called on a background thread.
        /// </summary>
        /// <param name="bytes">Raw file bytes.</param>
        /// <param name="onProgress">Progress callback (0..1). May be called frequently.</param>
        /// <returns>Decoded audio data.</returns>
        AudioData ReadWithProgress(byte[] bytes, Action<float> onProgress);
    }
}