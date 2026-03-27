using System.Collections.Generic;

namespace TheBirdOfHermes.Undo
{
    /// <summary>
    /// Undo command for applying a filter to one or more tracks.
    /// Stores the previous RawBytes + FileName for each affected track.
    /// </summary>
    public class ApplyFilterCommand : IUndoCommand
    {
        public string Description { get; }

        private readonly struct TrackSnapshot
        {
            public readonly AudioTrack Track;
            public readonly byte[] PreviousRawBytes;
            public readonly string PreviousFileName;

            public TrackSnapshot(AudioTrack track)
            {
                Track = track;
                PreviousRawBytes = track.RawBytes != null ? (byte[])track.RawBytes.Clone() : null;
                PreviousFileName = track.FileName;
            }
        }

        private readonly List<TrackSnapshot> _snapshots;
        private readonly Audio.AudioFilterBase _filter;

        /// <summary>
        /// Creates the command and captures current state of all tracks BEFORE the filter is applied.
        /// Call this before applying the filter, then call Execute() or let the caller apply the filter.
        /// </summary>
        public ApplyFilterCommand(string filterName, IEnumerable<AudioTrack> tracks, Audio.AudioFilterBase filter)
        {
            Description = $"Apply {filterName}";
            _filter = filter;
            _snapshots = new List<TrackSnapshot>();
            foreach (var track in tracks)
                _snapshots.Add(new TrackSnapshot(track));
        }

        public void Undo()
        {
            foreach (var snap in _snapshots)
            {
                if (snap.PreviousRawBytes != null)
                    snap.Track.RestoreFromBytes(snap.PreviousRawBytes, snap.PreviousFileName);
            }
        }

        public void Redo()
        {
            foreach (var snap in _snapshots)
                snap.Track.ApplyFilter(_filter);
        }
    }
}
