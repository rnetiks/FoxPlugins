using System;

namespace TheBirdOfHermes.Undo
{
    /// <summary>
    /// Generic undo command for simple property changes (float, string, bool, Color, enum).
    /// Captures old and new values, applies via setter action.
    /// </summary>
    public class PropertyCommand<T> : IUndoCommand
    {
        public string Description { get; }

        private readonly T _oldValue;
        private readonly T _newValue;
        private readonly Action<T> _setter;

        public PropertyCommand(string description, T oldValue, T newValue, Action<T> setter)
        {
            Description = description;
            _oldValue = oldValue;
            _newValue = newValue;
            _setter = setter;
        }

        public void Undo() => _setter(_oldValue);
        public void Redo() => _setter(_newValue);
    }
}
