using UnityEngine;

namespace TheBirdOfHermes.Undo
{
    /// <summary>
    /// Undo command for removing a track. Stores all metadata needed to re-create it.
    /// </summary>
    public class RemoveTrackCommand : IUndoCommand
    {
        public string Description { get; }

        private readonly TrackManager _manager;
        private readonly byte[] _rawBytes;
        private readonly string _fileName;
        private readonly string _displayName;
        private readonly float _offset;
        private readonly float _trimStart;
        private readonly float _trimEnd;
        private readonly float _fadeIn;
        private readonly float _fadeOut;
        private readonly Color _trackColor;
        private readonly WaveformMode _normMode;
        private readonly int _laneIndex;

        public RemoveTrackCommand(TrackManager manager, AudioTrack track)
        {
            _manager = manager;
            _rawBytes = track.RawBytes != null ? (byte[])track.RawBytes.Clone() : null;
            _fileName = track.FileName;
            _displayName = track.Name;
            _offset = track.Offset;
            _trimStart = track.TrimStart;
            _trimEnd = track.TrimEnd;
            _fadeIn = track.FadeInDuration;
            _fadeOut = track.FadeOutDuration;
            _trackColor = track.TrackColor;
            _normMode = track.NormalizationMode;
            _laneIndex = manager.GetLaneIndex(track.Lane);
            Description = $"Remove Track '{track.Name}'";
        }

        public void Undo()
        {
            if (_rawBytes == null) return;

            var track = _manager.AddTrackFromBytes(_rawBytes, _fileName);
            track.Name = _displayName;
            track.Offset = _offset;
            track.TrimStart = _trimStart;
            track.TrimEnd = _trimEnd;
            track.FadeInDuration = _fadeIn;
            track.FadeOutDuration = _fadeOut;
            track.TrackColor = _trackColor;
            track.NormalizationMode = _normMode;

            var targetLane = _manager.GetLaneAtIndex(_laneIndex);
            if (targetLane != null && targetLane != track.Lane)
                _manager.MoveTrackToLane(track, targetLane);
        }

        public void Redo()
        {
            foreach (var track in _manager.AllTracks)
            {
                if (track.FileName == _fileName &&
                    Mathf.Approximately(track.Offset, _offset) &&
                    track.Name == _displayName)
                {
                    _manager.RemoveTrack(track);
                    return;
                }
            }
        }
    }
}
