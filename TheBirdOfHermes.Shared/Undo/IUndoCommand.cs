namespace TheBirdOfHermes.Undo
{
    public interface IUndoCommand
    {
        string Description { get; }
        void Undo();
        void Redo();
    }
}
