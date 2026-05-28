namespace Vence.Storage;

public sealed record StoredDocument(Guid Id, string Path, string Content)
{
    public StoredDocument(string path, string content)
        : this(Guid.NewGuid(), path, content)
    {
    }
}
