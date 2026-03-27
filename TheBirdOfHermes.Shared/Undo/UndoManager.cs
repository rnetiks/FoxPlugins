using System.Collections.Generic;

namespace TheBirdOfHermes.Undo
{
    public class UndoManager
    {
        private readonly LinkedList<IUndoCommand> _undoStack = new LinkedList<IUndoCommand>();
        private readonly Stack<IUndoCommand> _redoStack = new Stack<IUndoCommand>();

        public int MaxUndoSteps { get; set; } = 20;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string UndoDescription => CanUndo ? _undoStack.Last.Value.Description : null;
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : null;

        /// <summary>
        /// Records a command that has already been executed.
        /// </summary>
        public void Push(IUndoCommand command)
        {
            _undoStack.AddLast(command);
            _redoStack.Clear();

            while (_undoStack.Count > MaxUndoSteps)
                _undoStack.RemoveFirst();
        }

        public void PerformUndo()
        {
            if (!CanUndo) return;

            var cmd = _undoStack.Last.Value;
            _undoStack.RemoveLast();
            cmd.Undo();
            _redoStack.Push(cmd);
        }

        public void PerformRedo()
        {
            if (!CanRedo) return;

            var cmd = _redoStack.Pop();
            cmd.Redo();
            _undoStack.AddLast(cmd);

            while (_undoStack.Count > MaxUndoSteps)
                _undoStack.RemoveFirst();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
