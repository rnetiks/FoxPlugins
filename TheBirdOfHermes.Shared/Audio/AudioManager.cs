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
        private ScratchAudio _scratchAudio;

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
        /// Loads audio data from a file, initializes the audio system, and sets up
        /// the internal state using the specified file path. This method reads the file
        /// bytes and processes the audio data to prepare for playback and further operations.
        /// </summary>
        /// <param name="path">The full path to the audio file to be loaded.</param>
        public void LoadFromFile(string path)
        {
            Clear();

            _rawBytes = File.ReadAllBytes(path);
            string extension = Path.GetExtension(path);
            _audioData = AudioLoader.Load(_rawBytes, extension);

            InitAudio(Path.GetFileName(path));
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

            _scratchAudio = _audioGO.AddComponent<ScratchAudio>();
            _scratchAudio.SetSamples(_audioData.Samples, _audioData.Channels, _audioData.SampleRate);

            FileName = fileName;
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
            _scratchAudio = null;
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
        /// Synchronizes the audio playback state with the given playback time, playing or pausing the audio source as needed.
        /// Adjusts the playback position and speed to maintain synchronization with the requested state, including optional handling of scrubbing effects.
        /// </summary>
        /// <param name="playbackTime">The desired playback time in seconds.</param>
        /// <param name="isPlaying">Boolean indicating whether playback should be active.</param>
        /// <param name="audioOffset">An offset to apply to the playback time, in seconds.</param>
        public void SyncPlayback(float playbackTime, bool isPlaying, float audioOffset)
        {
            if (_source == null || _audioData == null) return;

            if (!_isScrubbing)
            {
                if (isPlaying && !_source.isPlaying)
                {
                    _source.time = Mathf.Clamp(playbackTime + audioOffset, 0, _clip.length);
                    _source.Play();
                }
                else if (!isPlaying && _source.isPlaying)
                {
                    _source.Pause();
                }
                else if (isPlaying)
                {
                    float expectedTime = playbackTime + audioOffset;
                    if (Mathf.Abs(_source.time - expectedTime) > 0.1f)
                        _source.time = Mathf.Clamp(expectedTime, 0, _clip.length);
                }

                _source.pitch = Time.timeScale;
            }

            if (!_isScrubbing && Mathf.Abs(_scrubSpeed) > 0.01f)
            {
                _scrubSpeed *= 0.85f;
                if (Mathf.Abs(_scrubSpeed) < 0.01f)
                    _scrubSpeed = 0f;
            }

            if (_scratchAudio != null)
            {
                if (_isScrubbing || Mathf.Abs(_scrubSpeed) > 0.01f)
                {
                    _scratchAudio.SetSpeed(_scrubSpeed);
                    _scratchAudio.SetPosition(playbackTime + audioOffset);
                    _scratchAudio.SetActive(true);
                }
                else
                {
                    _scratchAudio.SetActive(false);
                }
            }

            _isScrubbing = false;
        }

        /// <summary>
        /// Scrubs the audio playback to a specific time with an optional offset, possibly enabling scratch sound effects.
        /// </summary>
        /// <param name="time">The target time to scrub to, in seconds.</param>
        /// <param name="audioOffset">An additional offset applied to the target scrub time, in seconds.</param>
        /// <param name="enableScratchSound">Whether to enable scratch sound effects during scrubbing.</param>
        public void Scrub(float time, float audioOffset, bool enableScratchSound)
        {
            if (_source == null || _audioData == null) return;

            if (enableScratchSound && _scratchAudio != null)
            {
                float deltaTime = time - _lastScrubTime;
                float frameTime = Time.unscaledDeltaTime > 0 ? Time.unscaledDeltaTime : 0.016f;
                _scrubSpeed = Mathf.Clamp(deltaTime / frameTime, -8f, 8f);
                _isScrubbing = true;
            }

            _lastScrubTime = time;

            if (!_isScrubbing)
                _source.time = Mathf.Clamp(time + audioOffset, 0, _clip.length);
        }

        /// <summary>
        /// Seeks the audio playback to a specified time with an offset applied.
        /// </summary>
        /// <param name="time">The target time to seek to, in seconds.</param>
        /// <param name="audioOffset">An offset applied to the target seek time, in seconds.</param>
        public void SeekTo(float time, float audioOffset)
        {
            if (_source == null) return;
            _lastScrubTime = time;
            _source.time = Mathf.Clamp(time + audioOffset, 0, _clip.length);
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