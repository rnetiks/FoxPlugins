namespace TheBirdOfHermes.Undo
{
    /// <summary>
    /// Undo command for moving a track between lanes.
    /// </summary>
    public class MoveToLaneCommand : IUndoCommand
    {
        public string Description => "Move Track to Lane";

        private readonly AudioTrack _track;
        private readonly AudioLane _oldLane;
        private readonly AudioLane _newLane;
        private readonly float _oldOffset;
        private readonly float _newOffset;
        private readonly TrackManager _manager;

        public MoveToLaneCommand(TrackManager manager, AudioTrack track, AudioLane oldLane, float oldOffset, AudioLane newLane, float newOffset)
        {
            _manager = manager;
            _track = track;
            _oldLane = oldLane;
            _newLane = newLane;
            _oldOffset = oldOffset;
            _newOffset = newOffset;
        }

        public void Undo()
        {
            _manager.MoveTrackToLane(_track, _oldLane);
            _track.Offset = _oldOffset;
        }

        public void Redo()
        {
            _manager.MoveTrackToLane(_track, _newLane);
            _track.Offset = _newOffset;
        }
    }
}
