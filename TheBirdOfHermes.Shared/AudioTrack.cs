using System;
using System.IO;
using TheBirdOfHermes.Audio;
using TheBirdOfHermes.Waveform;
using UnityEngine;

namespace TheBirdOfHermes
{
    public enum WaveformMode
    {
        Normal,
        Max,
        Volume
    }

    public class AudioTrack
    {
        public AudioManager Audio { get; private set; }
        public WaveformRenderer Waveform { get; private set; }

        public string Name { get; set; } = "";
        public string FileName { get; set; } = "";
        public byte[] RawBytes { get; private set; }

        public float Offset { get; set; }
        public float TrimStart { get; set; }
        public float TrimEnd { get; set; }
        public bool IsSelected { get; set; }
        public Color TrackColor { get; set; } = Color.white;

        public float FadeInDuration { get; set; }
        public float FadeOutDuration { get; set; }

        public WaveformMode NormalizationMode { get; set; } = WaveformMode.Normal;

        public AudioLane Lane { get; set; }

        private AsyncTrackOperation _asyncOp;

        /// <summary>True if a background decode or filter is in progress.</summary>
        public bool IsBusy => _asyncOp != null && !_asyncOp.IsComplete;

        /// <summary>True if a background decode is in progress.</summary>
        public bool IsLoading => IsBusy && _asyncOp.Type == AsyncTrackOperation.OperationType.Decode;

        /// <summary>True if a background filter is in progress.</summary>
        public bool IsFiltering => IsBusy && _asyncOp.Type == AsyncTrackOperation.OperationType.Filter;

        /// <summary>Progress of the current async operation (0..1).</summary>
        public float AsyncProgress => _asyncOp?.Progress ?? 0f;

        /// <summary>Description of the current async operation.</summary>
        public string AsyncDescription => _asyncOp?.Description ?? "";

        public float FullDuration => Audio?.Data?.Duration ?? 0f;
        public float EffectiveDuration => Mathf.Max(0f, FullDuration - TrimStart - TrimEnd);

        /// <summary>Visual start of the full track block (including trimmed regions).</summary>
        public float VisualStart => Offset;
        /// <summary>Visual end of the full track block (including trimmed regions).</summary>
        public float VisualEnd => Offset + FullDuration;
        /// <summary>Start of the audible (non-trimmed) region on the timeline.</summary>
        public float AudibleStart => Offset + TrimStart;
        /// <summary>End of the audible (non-trimmed) region on the timeline.</summary>
        public float AudibleEnd => Offset + FullDuration - TrimEnd;

        public float TimelineStart => AudibleStart;
        public float TimelineEnd => AudibleEnd;
        public bool HasAudio => Audio != null && Audio.HasAudio;

        private readonly MonoBehaviour _owner;

        public AudioTrack(MonoBehaviour owner)
        {
            _owner = owner;
            Audio = new AudioManager(owner);
            Waveform = new WaveformRenderer();
        }

        /// <summary>
        /// Synchronizes playback with the timeline. Volume comes from lane and master only.
        /// Applies fade in/out multiplier based on current position.
        /// </summary>
        public void SyncPlayback(float playbackTime, bool isPlaying, float laneVolume, float masterVolume, bool laneMuted)
        {
            if (!HasAudio || Audio.Source == null) return;

            float fadeMultiplier = GetFadeMultiplier(playbackTime);
            float effectiveVolume = laneMuted ? 0f : laneVolume * masterVolume * fadeMultiplier;
            Audio.Source.volume = effectiveVolume;

            bool inRange = playbackTime >= AudibleStart && playbackTime < AudibleEnd;

            if (isPlaying && inRange)
            {
                float audioTime = (playbackTime - Offset);
                audioTime = Mathf.Clamp(audioTime, TrimStart, FullDuration - TrimEnd);

                if (!Audio.Source.isPlaying)
                {
                    Audio.Source.time = audioTime;
                    Audio.Source.Play();
                }
                else
                {
                    if (Mathf.Abs(Audio.Source.time - audioTime) > 0.1f)
                        Audio.Source.time = audioTime;

                    if (Audio.Source.time >= FullDuration - TrimEnd)
                        Audio.Source.Pause();
                }

                Audio.Source.pitch = Time.timeScale;
            }
            else
            {
                if (Audio.Source.isPlaying)
                    Audio.Source.Pause();
            }
        }

        /// <summary>
        /// Calculates the fade multiplier (0-1) for a given timeline playback time.
        /// </summary>
        private float GetFadeMultiplier(float playbackTime)
        {
            if (playbackTime < AudibleStart || playbackTime >= AudibleEnd)
                return 0f;

            float posInAudible = playbackTime - AudibleStart;
            float audibleDuration = EffectiveDuration;

            if (FadeInDuration > 0f && posInAudible < FadeInDuration)
                return Mathf.Clamp01(posInAudible / FadeInDuration);

            float fadeOutStart = audibleDuration - FadeOutDuration;
            if (FadeOutDuration > 0f && posInAudible > fadeOutStart)
                return Mathf.Clamp01((audibleDuration - posInAudible) / FadeOutDuration);

            return 1f;
        }

        public void SeekTo(float playbackTime)
        {
            if (!HasAudio || Audio.Source == null) return;

            bool inRange = playbackTime >= AudibleStart && playbackTime < AudibleEnd;
            if (inRange)
            {
                float audioTime = (playbackTime - Offset);
                audioTime = Mathf.Clamp(audioTime, TrimStart, FullDuration - TrimEnd);
                Audio.Source.time = audioTime;
            }
        }

        public void ApplyFilter(IAudioFilter filter)
        {
            if (!HasAudio) return;

            if (filter is AudioFilterBase filterBase)
            {
                ApplyFilterAsync(filterBase);
                return;
            }

            filter.Process(Audio.Data);
            Audio.ReloadFromData();
            RawBytes = Audio.Data.EncodeWav();
            FileName = System.IO.Path.ChangeExtension(FileName, ".wav");
            Waveform.SetSamples(Audio.MonoSamples, Audio.Data.SampleRate);
            ClampTrim();
            ClampFade();
        }

        /// <summary>
        /// Starts an async filter operation on a background thread.
        /// </summary>
        private void ApplyFilterAsync(AudioFilterBase filter)
        {
            if (IsBusy) return;
            _asyncOp = AsyncTrackOperation.StartFilter(Audio.Data, filter, filter.Name);
        }

        /// <summary>
        /// Starts an async decode operation. RawBytes and FileName are set immediately.
        /// The actual decoding runs on a background thread.
        /// </summary>
        public void LoadFromBytesAsync(byte[] audioBytes, string fileName)
        {
            Clear();
            RawBytes = audioBytes;
            FileName = fileName;
            Name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            _asyncOp = AsyncTrackOperation.StartDecode(audioBytes, extension, fileName);
        }

        /// <summary>
        /// Starts an async decode operation from a file path.
        /// </summary>
        public void LoadFromFileAsync(string path)
        {
            Clear();
            var bytes = File.ReadAllBytes(path);
            RawBytes = bytes;
            FileName = Path.GetFileName(path);
            Name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            _asyncOp = AsyncTrackOperation.StartDecode(bytes, extension, FileName);
        }

        /// <summary>
        /// Polls the current async operation. Must be called from the main thread (Update).
        /// Returns true if the operation just completed this frame.
        /// </summary>
        public bool PollAsyncCompletion()
        {
            if (_asyncOp == null || !_asyncOp.IsComplete) return false;

            var op = _asyncOp;
            _asyncOp = null;

            if (op.Error != null)
            {
                Entry.Logger.LogError($"Async {op.Type} failed for '{op.Description}': {op.Error}");
                return true;
            }

            switch (op.Type)
            {
                case AsyncTrackOperation.OperationType.Decode:
                    Audio.InitFromDecodedData(op.Result, FileName);
                    Waveform.SetSamples(Audio.MonoSamples, Audio.Data.SampleRate);
                    break;

                case AsyncTrackOperation.OperationType.Filter:
                    Audio.ReloadFromData();
                    RawBytes = Audio.Data.EncodeWav();
                    FileName = Path.ChangeExtension(FileName, ".wav");
                    Waveform.SetSamples(Audio.MonoSamples, Audio.Data.SampleRate);
                    ClampTrim();
                    ClampFade();
                    break;
            }

            return true;
        }

        /// <summary>
        /// Restores track audio from previously saved RawBytes (used by undo).
        /// Re-decodes and rebuilds waveform.
        /// </summary>
        public void RestoreFromBytes(byte[] rawBytes, string fileName)
        {
            if (rawBytes == null) return;
            Audio.LoadFromBytes(rawBytes, fileName);
            RawBytes = rawBytes;
            FileName = fileName;
            Waveform.SetSamples(Audio.MonoSamples, Audio.Data.SampleRate);
            ClampTrim();
            ClampFade();
        }

        public void Clear()
        {
            Audio?.Clear();
            Waveform?.Clear();
            RawBytes = null;
            TrimStart = 0f;
            TrimEnd = 0f;
            FadeInDuration = 0f;
            FadeOutDuration = 0f;
        }

        public void Destroy()
        {
            Clear();
            Audio = null;
            Waveform = null;
        }

        public void ClampTrim()
        {
            if (!HasAudio) return;
            TrimStart = Mathf.Clamp(TrimStart, 0f, FullDuration - 0.01f);
            TrimEnd = Mathf.Clamp(TrimEnd, 0f, FullDuration - TrimStart - 0.01f);
        }

        public void ClampFade()
        {
            FadeInDuration = Mathf.Clamp(FadeInDuration, 0f, EffectiveDuration);
            FadeOutDuration = Mathf.Clamp(FadeOutDuration, 0f, EffectiveDuration);
            if (FadeInDuration + FadeOutDuration > EffectiveDuration)
            {
                float ratio = EffectiveDuration / (FadeInDuration + FadeOutDuration);
                FadeInDuration *= ratio;
                FadeOutDuration *= ratio;
            }
        }
    }
}