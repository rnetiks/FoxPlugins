using System.Collections.Generic;

namespace TheBirdOfHermes.Undo
{
    /// <summary>
    /// Undo command for moving one or more selected tracks (offset change).
    /// </summary>
    public class MultiMoveCommand : IUndoCommand
    {
        public string Description { get; }

        private readonly Dictionary<AudioTrack, float> _oldOffsets;
        private readonly Dictionary<AudioTrack, float> _newOffsets;

        public MultiMoveCommand(string description, Dictionary<AudioTrack, float> oldOffsets, Dictionary<AudioTrack, float> newOffsets)
        {
            Description = description;
            _oldOffsets = new Dictionary<AudioTrack, float>(oldOffsets);
            _newOffsets = new Dictionary<AudioTrack, float>(newOffsets);
        }

        public void Undo()
        {
            foreach (var kvp in _oldOffsets)
                kvp.Key.Offset = kvp.Value;
        }

        public void Redo()
        {
            foreach (var kvp in _newOffsets)
                kvp.Key.Offset = kvp.Value;
        }
    }
}
