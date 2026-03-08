namespace TheBirdOfHermes.Audio
{
    public class AudioData
    {
        /// <summary>
        /// Represents an array of sample data for audio processing. Each element in the array corresponds to an audio sample,
        /// with the number of samples depending on the audio's duration, sample rate, and number of channels.
        /// </summary>
        public float[] Samples;
        /// <summary>
        /// Represents the number of audio samples processed or played per second.
        /// Determines the precision and quality of the audio data.
        /// </summary>
        public int SampleRate;
        /// <summary>
        /// Specifies the number of audio channels present in the audio data. A value of 1 indicates mono audio, while 2 indicates stereo.
        /// This value is used in conjunction with the sample rate and duration to determine the structure and processing of the audio data.
        /// </summary>
        public int Channels;
        /// <summary>
        /// Represents the total duration of the audio data in seconds.
        /// Calculated based on the length of the sample array, sample rate, and number of channels.
        /// </summary>
        public float Duration => Samples.Length / (float)(SampleRate * Channels);
    }
}