namespace TheBirdOfHermes.Undo
{
    /// <summary>
    /// Undo command for adding a track. Undo removes it, redo re-adds it.
    /// </summary>
    public class AddTrackCommand : IUndoCommand
    {
        public string Description { get; }

        private readonly TrackManager _manager;
        private readonly AudioTrack _track;
        private readonly byte[] _rawBytes;
        private readonly string _fileName;

        public AddTrackCommand(TrackManager manager, AudioTrack track)
        {
            _manager = manager;
            _track = track;
            _rawBytes = track.RawBytes != null ? (byte[])track.RawBytes.Clone() : null;
            _fileName = track.FileName;
            Description = $"Add Track '{track.Name}'";
        }

        public void Undo()
        {
            _manager.RemoveTrack(_track);
        }

        public void Redo()
        {
            _manager.AddTrackFromBytes(_rawBytes, _fileName);
        }
    }
}
