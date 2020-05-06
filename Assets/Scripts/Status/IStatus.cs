namespace MyGame
{
    public interface IStatus<T> where T : struct
    {
        T Current { get; }
        T Max { get; }
        bool IsEmpty { get; }

        void FullRestore();
        void Clear();
        void Restore(T value);
        void Remove(T value);
    }
}
