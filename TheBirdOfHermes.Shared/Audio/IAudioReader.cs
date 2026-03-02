namespace TheBirdOfHermes.Audio
{
    public interface IAudioReader
    {
        string[] SupportedExtensions { get; }
        bool CanRead(byte[] headerBytes);
        AudioData Read(byte[] bytes);
    }
}