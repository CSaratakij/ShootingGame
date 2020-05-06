namespace MyGame
{
    public class Status : IStatus<int>
    {
        public int Current => current;
        public int Max => max;
        public bool IsEmpty => (current <= 0);

        int current;
        int max;

        public Status(int current, int max)
        {
            this.current = current;
            this.max = max;
        }

        public void Clear()
        {
            current = 0;
        }

        public void FullRestore()
        {
            current = max;
        }

        public void Remove(int value)
        {
            current = (current - value) < 0 ? 0 : (current - value);
        }

        public void Restore(int value)
        {
            current = (current + value) > max ? max : (current + value);
        }
    }
}
