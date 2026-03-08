using System.Collections.Generic;
using TheBirdOfHermes.Audio;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes
{
    public class TrackManager
    {
        private readonly MonoBehaviour _owner;
        private readonly List<AudioTrack> _tracks = new List<AudioTrack>();
        private int _nextColorIndex;

        // public IEnumerable<AudioTrack> Tracks => _tracks;
        public List<AudioTrack> Tracks => _tracks;
        // public IReadOnlyList<AudioTrack> Tracks => (IReadOnlyList<AudioTrack>)_tracks;
        public float MasterVolume { get; set; } = 1f;
        public AudioTrack SelectedTrack { get; private set; }
        public float SnapThreshold { get; set; } = 0.1f;
        // public bool SnapEnabled { get; set; } = true;

        public TrackManager(MonoBehaviour owner)
        {
            _owner = owner;
        }

        public AudioTrack AddTrackFromFile(string path)
        {
            var track = new AudioTrack(_owner);
            track.LoadFromFile(path);
            track.TrackColor = WindowStyles.GetTrackColor(_nextColorIndex++);
            _tracks.Add(track);

            if (SelectedTrack == null)
                SelectTrack(track);

            return track;
        }

        public AudioTrack AddTrackFromBytes(byte[] audioBytes, string fileName)
        {
            var track = new AudioTrack(_owner);
            track.LoadFromBytes(audioBytes, fileName);
            track.TrackColor = WindowStyles.GetTrackColor(_nextColorIndex++);
            _tracks.Add(track);

            if (SelectedTrack == null)
                SelectTrack(track);

            return track;
        }

        public void RemoveTrack(AudioTrack track)
        {
            if (track == null) return;

            _tracks.Remove(track);
            track.Destroy();

            if (SelectedTrack == track)
                SelectedTrack = _tracks.Count > 0 ? _tracks[0] : null;
        }

        public void ClearAll()
        {
            foreach (var track in _tracks)
                track.Destroy();
            _tracks.Clear();
            SelectedTrack = null;
            _nextColorIndex = 0;
        }

        public void SelectTrack(AudioTrack track)
        {
            if (SelectedTrack != null)
                SelectedTrack.IsSelected = false;

            SelectedTrack = track;

            if (track != null)
                track.IsSelected = true;
        }

        public void MoveTrack(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _tracks.Count) return;
            toIndex = Mathf.Clamp(toIndex, 0, _tracks.Count - 1);
            if (fromIndex == toIndex) return;

            var track = _tracks[fromIndex];
            _tracks.RemoveAt(fromIndex);
            _tracks.Insert(toIndex, track);
        }

        public void SyncAllPlayback(float playbackTime, bool isPlaying)
        {
            foreach (var track in _tracks)
                track.SyncPlayback(playbackTime, isPlaying, MasterVolume);
        }

        public void SeekAll(float playbackTime)
        {
            foreach (var track in _tracks)
                track.SeekTo(playbackTime);
        }

        public float TrySnap(AudioTrack dragging, float proposedOffset)
        {
            // if (!SnapEnabled) return proposedOffset;
            // Switched to key press, rather than a button
            if (!Input.GetKey(KeyCode.LeftShift)) return proposedOffset;

            float bestOffset = proposedOffset;
            float bestDist = SnapThreshold;

            float dragStart = proposedOffset;
            float dragEnd = proposedOffset + dragging.EffectiveDuration;

            foreach (var other in _tracks)
            {
                if (other == dragging) continue;

                float d = Mathf.Abs(dragStart - other.TimelineStart);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestOffset = other.TimelineStart;
                }

                d = Mathf.Abs(dragStart - other.TimelineEnd);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestOffset = other.TimelineEnd;
                }

                d = Mathf.Abs(dragEnd - other.TimelineStart);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestOffset = proposedOffset + (other.TimelineStart - dragEnd);
                }

                d = Mathf.Abs(dragEnd - other.TimelineEnd);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestOffset = proposedOffset + (other.TimelineEnd - dragEnd);
                }
            }

            if (Mathf.Abs(proposedOffset) < bestDist)
                bestOffset = 0f;

            return Mathf.Max(0f, bestOffset);
        }

        public List<float> GetSnapLines(AudioTrack excluding)
        {
            var lines = new List<float>();
            foreach (var track in _tracks)
            {
                if (track == excluding) continue;
                lines.Add(track.TimelineStart);
                lines.Add(track.TimelineEnd);
            }
            return lines;
        }

        public bool HasAudio => _tracks.Count > 0;
        public int TrackCount => _tracks.Count;
    }
}
