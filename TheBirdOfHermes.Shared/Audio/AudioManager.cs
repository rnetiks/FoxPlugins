using System;
using System.IO;
using UnityEngine;

namespace TheBirdOfHermes.Audio
{
    public class AudioManager
    {
        private readonly MonoBehaviour _owner;

        private GameObject _audioGO;
        private AudioClip _clip;
        private AudioSource _source;

        private AudioData _audioData;
        private byte[] _rawBytes;
        private float[] _monoSamples;

        private bool _isScrubbing;
        private float _lastScrubTime;
        private float _scrubSpeed;

        public string FileName { get; private set; } = "";
        public bool HasAudio => _audioData != null;
        public AudioData Data => _audioData;
        public byte[] RawBytes => _rawBytes;
        public float[] MonoSamples => _monoSamples;
        public AudioSource Source => _source;
        public AudioClip Clip => _clip;

        public AudioManager(MonoBehaviour owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Loads audio data from a given byte array and initializes the audio system using
        /// the specified file name. The method processes the provided audio bytes and prepares
        /// the internal state for playback and further operations.
        /// </summary>
        /// <param name="audioBytes">The raw audio data in byte array format to be loaded.</param>
        /// <param name="fileName">The name of the file associated with the audio bytes, including the extension.</param>
        public void LoadFromBytes(byte[] audioBytes, string fileName)
        {
            Clear();

            _rawBytes = audioBytes;
            string extension = Path.GetExtension(fileName);
            _audioData = !string.IsNullOrEmpty(extension)
                ? AudioLoader.Load(audioBytes, extension)
                : AudioLoader.Load(audioBytes);

            InitAudio(fileName);
        }

        /// <summary>
        /// Initializes the audio system by preparing necessary components such as the audio clip,
        /// audio GameObject, AudioSource, and ScratchAudio. It sets up the audio clip using provided
        /// audio data and assigns it to an AudioSource for playback. Also, converts multi-channel
        /// audio data to mono if necessary and stores the provided file name for reference.
        /// </summary>
        /// <param name="fileName">The name of the audio file used for identification.</param>
        private void InitAudio(string fileName)
        {
            _monoSamples = _audioData.Channels == 1
                ? _audioData.Samples
                : MixToMono(_audioData.Samples, _audioData.Channels);

            _clip = AudioClip.Create(fileName, _audioData.Samples.Length / _audioData.Channels,
                _audioData.Channels, _audioData.SampleRate, false);
            _clip.SetData(_audioData.Samples, 0);

            _audioGO = new GameObject($"TBOH_Audio_{fileName}");
            _audioGO.transform.SetParent(_owner.transform);

            _source = _audioGO.AddComponent<AudioSource>();
            _source.clip = _clip;
            _source.loop = false;

            FileName = fileName;
        }

        /// <summary>
        /// Initializes the audio system from pre-decoded AudioData.
        /// Called on the main thread after an async decode completes.
        /// </summary>
        public void InitFromDecodedData(AudioData audioData, string fileName)
        {
            Clear();
            _audioData = audioData;
            InitAudio(fileName);
        }

        public void ReloadFromData()
        {
            if (_audioData == null) return;

            _monoSamples = _audioData.Channels == 1
                ? _audioData.Samples
                : MixToMono(_audioData.Samples, _audioData.Channels);

            if (_clip != null)
                UnityEngine.Object.Destroy(_clip);

            _clip = AudioClip.Create(FileName, _audioData.Samples.Length / _audioData.Channels,
                _audioData.Channels, _audioData.SampleRate, false);
            _clip.SetData(_audioData.Samples, 0);

            if (_source != null)
            {
                _source.Stop();
                _source.clip = _clip;
            }
        }

        /// <summary>
        /// Resets the audio manager by stopping and clearing any active audio source,
        /// destroying relevant audio objects such as the audio GameObject and clip,
        /// and removing references to loaded audio data and raw bytes.
        /// Additionally, resets the file name and any associated audio variables to their default state.
        /// </summary>
        public void Clear()
        {
            if (_source != null)
            {
                _source.Stop();
                _source = null;
            }

            if (_audioGO != null)
            {
                UnityEngine.Object.Destroy(_audioGO);
                _audioGO = null;
            }
            if (_clip != null)
            {
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
            }
            _audioData = null;
            _rawBytes = null;
            _monoSamples = null;
            FileName = "";
        }

        /// <summary>
        /// Converts multi-channel audio samples to mono by averaging the values of each channel.
        /// </summary>
        /// <param name="samples">An array of audio samples containing all channels.</param>
        /// <param name="channels">The number of audio channels in the sample data.</param>
        /// <returns>An array of mono audio samples.</returns>
        private static float[] MixToMono(float[] samples, int channels)
        {
            float[] mono = new float[samples.Length / channels];
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += samples[i * channels + c];
                mono[i] = sum / channels;
            }
            return mono;
        }
    }
}