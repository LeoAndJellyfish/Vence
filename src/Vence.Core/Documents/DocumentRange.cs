namespace Vence.Core.Documents;

public sealed record DocumentRange
{
    public DocumentRange(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Range start cannot be negative.");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "Range end cannot be before start.");
        }

        Start = start;
        End = end;
    }

    public int Start { get; }

    public int End { get; }

    public int Length => End - Start;
}
