using System.Collections.Generic;
using System.Linq;
using BepInEx;
using TheBirdOfHermes.UI;
using UnityEngine;

namespace TheBirdOfHermes
{
    public class TrackManager
    {
        private readonly MonoBehaviour _owner;
        private readonly List<AudioLane> _lanes = new List<AudioLane>();
        private int _nextColorIndex;

        public List<AudioLane> Lanes => _lanes;
        public IEnumerable<AudioTrack> AllTracks => _lanes.SelectMany(l => l.Tracks);
        public float MasterVolume { get; set; } = 1f;

        public HashSet<AudioTrack> SelectedTracks { get; } = new HashSet<AudioTrack>();
        public AudioTrack PrimarySelectedTrack { get; private set; }

        public int SnapPixelDistance { get; set; } = 10;

        public TrackManager(MonoBehaviour owner)
        {
            _owner = owner;
            EnsureEmptyLane();
        }

        public AudioTrack AddTrackFromFile(string path)
        {
            var lane = new AudioLane();
            var track = new AudioTrack(_owner);
            track.LoadFromFile(path);
            track.TrackColor = WindowStyles.GetTrackColor(_nextColorIndex++);
            track.Lane = lane;
            lane.Tracks.Add(track);

            int insertIndex = _lanes.Count > 0 && _lanes[_lanes.Count - 1].Tracks.Count == 0
                ? _lanes.Count - 1
                : _lanes.Count;
            _lanes.Insert(insertIndex, lane);

            if (PrimarySelectedTrack == null)
                SelectTrack(track, false);

            EnsureEmptyLane();
            return track;
        }

        public AudioTrack AddTrackFromBytes(byte[] audioBytes, string fileName)
        {
            var lane = new AudioLane();
            var track = new AudioTrack(_owner);
            track.LoadFromBytes(audioBytes, fileName);
            track.TrackColor = WindowStyles.GetTrackColor(_nextColorIndex++);
            track.Lane = lane;
            lane.Tracks.Add(track);

            int insertIndex = _lanes.Count > 0 && _lanes[_lanes.Count - 1].Tracks.Count == 0
                ? _lanes.Count - 1
                : _lanes.Count;
            _lanes.Insert(insertIndex, lane);

            if (PrimarySelectedTrack == null)
                SelectTrack(track, false);

            EnsureEmptyLane();
            return track;
        }

        public void RemoveTrack(AudioTrack track)
        {
            if (track == null) return;

            var lane = track.Lane;
            lane?.Tracks.Remove(track);
            track.Destroy();

            SelectedTracks.Remove(track);
            if (PrimarySelectedTrack == track)
                PrimarySelectedTrack = SelectedTracks.Count > 0 ? SelectedTracks.First() : null;

            RemoveEmptyLanesExceptLast();
            EnsureEmptyLane();
        }

        public void RemoveSelectedTracks()
        {
            var toRemove = SelectedTracks.ToList();
            SelectedTracks.Clear();
            PrimarySelectedTrack = null;

            foreach (var track in toRemove)
            {
                track.Lane?.Tracks.Remove(track);
                track.Destroy();
            }

            RemoveEmptyLanesExceptLast();
            EnsureEmptyLane();
        }

        public void ClearAll()
        {
            foreach (var lane in _lanes)
            foreach (var track in lane.Tracks)
                track.Destroy();

            _lanes.Clear();
            SelectedTracks.Clear();
            PrimarySelectedTrack = null;
            _nextColorIndex = 0;
            EnsureEmptyLane();
        }

        public void SelectTrack(AudioTrack track, bool additive)
        {
            if (!additive)
            {
                foreach (var t in SelectedTracks)
                    t.IsSelected = false;
                SelectedTracks.Clear();
            }

            if (track != null)
            {
                if (additive && SelectedTracks.Contains(track))
                {
                    track.IsSelected = false;
                    SelectedTracks.Remove(track);
                    PrimarySelectedTrack = SelectedTracks.Count > 0 ? SelectedTracks.First() : null;
                }
                else
                {
                    track.IsSelected = true;
                    SelectedTracks.Add(track);
                    PrimarySelectedTrack = track;
                }
            }
        }

        public void DeselectAll()
        {
            foreach (var track in SelectedTracks)
                track.IsSelected = false;
            SelectedTracks.Clear();
            PrimarySelectedTrack = null;
        }

        /// <summary>
        /// Moves a track to a target lane. Handles overlap clamping on the target lane.
        /// </summary>
        public void MoveTrackToLane(AudioTrack track, AudioLane targetLane)
        {
            if (track.Lane == targetLane) return;

            var oldLane = track.Lane;
            oldLane?.Tracks.Remove(track);

            track.Lane = targetLane;
            targetLane.Tracks.Add(track);

            ClampTrackPosition(track, targetLane);

            RemoveEmptyLanesExceptLast();
            EnsureEmptyLane();
        }

        /// <summary>
        /// Clamps a track's offset so it doesn't overlap with other tracks on the same lane.
        /// </summary>
        public void ClampTrackPosition(AudioTrack track, AudioLane lane)
        {
            foreach (var other in lane.Tracks)
            {
                if (other == track) continue;

                if (track.AudibleStart < other.AudibleEnd && track.AudibleEnd > other.AudibleStart)
                {
                    float distToEnd = Mathf.Abs(track.AudibleStart - other.AudibleEnd);
                    float distToStart = Mathf.Abs(track.AudibleEnd - other.AudibleStart);

                    if (distToEnd <= distToStart)
                    {
                        track.Offset = other.AudibleEnd - track.TrimStart;
                    }
                    else
                    {
                        track.Offset = other.AudibleStart - track.EffectiveDuration - track.TrimStart;
                    }

                    track.Offset = Mathf.Max(0f, track.Offset);
                }
            }
        }

        public int GetLaneIndex(AudioLane lane) => _lanes.IndexOf(lane);

        public AudioLane GetLaneAtIndex(int index)
        {
            if (index < 0 || index >= _lanes.Count) return null;
            return _lanes[index];
        }

        public void SyncAllPlayback(float playbackTime, bool isPlaying)
        {
            foreach (var lane in _lanes)
            foreach (var track in lane.Tracks)
                track.SyncPlayback(playbackTime, isPlaying, lane.Volume, MasterVolume, lane.IsMuted);
        }

        public void SeekAll(float playbackTime)
        {
            foreach (var lane in _lanes)
            foreach (var track in lane.Tracks)
                track.SeekTo(playbackTime);
        }

        /// <summary>
        /// Pixel-based snapping. Converts pixel threshold to time threshold using pxPerSecond.
        /// </summary>
        public float TrySnap(AudioTrack dragging, float proposedOffset, float pxPerSecond)
        {
            float timeThreshold = SnapPixelDistance / pxPerSecond;
            float bestOffset = proposedOffset;
            float bestDist = timeThreshold;

            float dragAudibleStart = proposedOffset + dragging.TrimStart;
            float dragAudibleEnd = proposedOffset + dragging.FullDuration - dragging.TrimEnd;

            foreach (var lane in _lanes)
            {
                foreach (var other in lane.Tracks)
                {
                    if (other == dragging) continue;
                    if (SelectedTracks.Contains(other)) continue;

                    float d = Mathf.Abs(dragAudibleStart - other.AudibleStart);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestOffset = other.AudibleStart - dragging.TrimStart;
                    }

                    d = Mathf.Abs(dragAudibleStart - other.AudibleEnd);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestOffset = other.AudibleEnd - dragging.TrimStart;
                    }

                    d = Mathf.Abs(dragAudibleEnd - other.AudibleStart);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestOffset = proposedOffset + (other.AudibleStart - dragAudibleEnd);
                    }

                    d = Mathf.Abs(dragAudibleEnd - other.AudibleEnd);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestOffset = proposedOffset + (other.AudibleEnd - dragAudibleEnd);
                    }
                }
            }

            if (Mathf.Abs(proposedOffset) < bestDist)
                bestOffset = 0f;

            // return Mathf.Max(0f, bestOffset);
            return bestOffset;
        }

        /// <summary>
        /// Multi-element snap: snaps based on the dragged track, then returns the delta to apply to all selected tracks.
        /// </summary>
        public float TrySnapMulti(AudioTrack dragged, float proposedOffset, float pxPerSecond, out float snapDelta)
        {
            float snappedOffset = TrySnap(dragged, proposedOffset, pxPerSecond);
            snapDelta = snappedOffset - proposedOffset;
            return snappedOffset;
        }

        public List<float> GetSnapLines(AudioTrack excluding)
        {
            var lines = new List<float>();
            foreach (var lane in _lanes)
            {
                foreach (var track in lane.Tracks)
                {
                    if (track == excluding) continue;
                    if (SelectedTracks.Contains(track)) continue;
                    lines.Add(track.AudibleStart);
                    lines.Add(track.AudibleEnd);
                }
            }
            return lines;
        }

        private void EnsureEmptyLane()
        {
            while (_lanes.Count > 1 &&
                   _lanes[_lanes.Count - 1].Tracks.Count == 0 &&
                   _lanes[_lanes.Count - 2].Tracks.Count == 0)
            {
                _lanes.RemoveAt(_lanes.Count - 1);
            }

            if (_lanes.Count == 0 || _lanes[_lanes.Count - 1].Tracks.Count > 0)
                _lanes.Add(new AudioLane());
        }

        private void RemoveEmptyLanesExceptLast()
        {
            for (int i = _lanes.Count - 1; i >= 0; i--)
            {
                if (_lanes[i].Tracks.Count == 0 && i < _lanes.Count - 1)
                    _lanes.RemoveAt(i);
            }
        }

        public bool HasAudio => _lanes.Any(l => l.Tracks.Count > 0);
        public int TrackCount => _lanes.Sum(l => l.Tracks.Count);
        public int LaneCount => _lanes.Count;
    }
}