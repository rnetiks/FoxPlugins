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

        public void LoadFromFile(string path)
        {
            Clear();

            _rawBytes = File.ReadAllBytes(path);
            string extension = Path.GetExtension(path);
            _audioData = AudioLoader.Load(_rawBytes, extension);

            InitAudio(Path.GetFileName(path));
        }

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

        public void SeekTo(float time, float audioOffset)
        {
            if (_source == null) return;
            _lastScrubTime = time;
            _source.time = Mathf.Clamp(time + audioOffset, 0, _clip.length);
        }

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