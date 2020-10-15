namespace OpenActive.FakeDatabase.NET.Helpers
{
    public readonly struct Bounds
    {
        public Bounds(int lower, int upper)
        {
            Lower = lower;
            Upper = upper;
        }

        public int Lower { get; }
        public int Upper { get; }
    }
}