namespace Vence.Core.Documents;

public sealed class Document
{
    public Document(Guid id, string? path, string content)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Document id cannot be empty.", nameof(id)) : id;
        Path = path;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public Guid Id { get; }

    public string? Path { get; }

    public string Content { get; private set; }

    public void Replace(DocumentRange range, string replacement)
    {
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(replacement);

        if (range.End > Content.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(range), "Range exceeds the document content length.");
        }

        Content = string.Concat(
            Content.AsSpan(0, range.Start),
            replacement,
            Content.AsSpan(range.End));
    }
}
