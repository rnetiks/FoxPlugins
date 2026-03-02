using System;
using System.IO;
using TheBirdOfHermes.Audio;
using TheBirdOfHermes.Waveform;
using UnityEngine;

namespace TheBirdOfHermes
{
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
        public float Volume { get; set; } = 1f;
        public bool IsSelected { get; set; }
        public bool IsMuted { get; set; }
        public Color TrackColor { get; set; } = Color.white;

        public float FullDuration => Audio?.Data?.Duration ?? 0f;
        public float EffectiveDuration => Mathf.Max(0f, FullDuration - TrimStart - TrimEnd);
        public float TimelineStart => Offset;
        public float TimelineEnd => Offset + EffectiveDuration;
        public bool HasAudio => Audio != null && Audio.HasAudio;

        private readonly MonoBehaviour _owner;

        public AudioTrack(MonoBehaviour owner)
        {
            _owner = owner;
            Audio = new AudioManager(owner);
            Waveform = new WaveformRenderer();
        }

        public void LoadFromFile(string path)
        {
            Clear();
            Audio.LoadFromFile(path);
            RawBytes = Audio.RawBytes;
            FileName = Path.GetFileName(path);
            Name = Path.GetFileNameWithoutExtension(path);
            Waveform.SetSamples(Audio.MonoSamples, Audio.Data.SampleRate);
        }

        public void LoadFromBytes(byte[] audioBytes, string fileName)
        {
            Clear();
            Audio.LoadFromBytes(audioBytes, fileName);
            RawBytes = audioBytes;
            FileName = fileName;
            Name = Path.GetFileNameWithoutExtension(fileName);
            Waveform.SetSamples(Audio.MonoSamples, Audio.Data.SampleRate);
        }

        public void SyncPlayback(float playbackTime, bool isPlaying, float masterVolume)
        {
            if (!HasAudio || Audio.Source == null) return;

            float effectiveVolume = IsMuted ? 0f : Volume * masterVolume;
            Audio.Source.volume = effectiveVolume;

            bool inRange = playbackTime >= TimelineStart && playbackTime < TimelineEnd;

            if (isPlaying && inRange)
            {
                float audioTime = (playbackTime - Offset) + TrimStart;
                audioTime = Mathf.Clamp(audioTime, TrimStart, FullDuration - TrimEnd);

                if (!Audio.Source.isPlaying)
                {
                    Audio.Source.time = audioTime;
                    Audio.Source.Play();
                }
                else
                {
                    float expectedTime = audioTime;
                    if (Mathf.Abs(Audio.Source.time - expectedTime) > 0.1f)
                        Audio.Source.time = expectedTime;

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

        public void SeekTo(float playbackTime)
        {
            if (!HasAudio || Audio.Source == null) return;

            bool inRange = playbackTime >= TimelineStart && playbackTime < TimelineEnd;
            if (inRange)
            {
                float audioTime = (playbackTime - Offset) + TrimStart;
                audioTime = Mathf.Clamp(audioTime, TrimStart, FullDuration - TrimEnd);
                Audio.Source.time = audioTime;
            }
        }

        public void Clear()
        {
            Audio?.Clear();
            Waveform?.Clear();
            RawBytes = null;
            TrimStart = 0f;
            TrimEnd = 0f;
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
    }
}
