namespace TheBirdOfHermes.Audio
{
    public interface IAudioReader
    {
        /// Gets a list of file extensions supported by the audio reader.
        /// Each extension is represented as a string, typically starting with a dot (e.g., ".wav", ".ogg").
        /// This property is used to determine whether a specific audio file format
        /// can be processed by the implementing audio reader.
        string[] SupportedExtensions { get; }
        /// Determines if the specified byte array matches the audio format that the reader can handle.
        /// <param name="headerBytes">The byte array containing the header of the audio file to check.</param>
        /// <returns>True if the byte array matches the supported format; otherwise, false.</returns>
        bool CanRead(byte[] headerBytes);
        /// Reads audio data from the provided byte array and processes it into an AudioData instance.
        /// <param name="bytes">The byte array containing the raw audio data to read.</param>
        /// <returns>An AudioData object containing the processed audio samples, sample rate, and channel information.</returns>
        AudioData Read(byte[] bytes);
    }
}