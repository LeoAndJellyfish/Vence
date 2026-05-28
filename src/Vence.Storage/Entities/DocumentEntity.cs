namespace Vence.Storage.Entities;

public sealed class DocumentEntity
{
    public Guid Id { get; set; }

    public required string Path { get; set; }

    public required string Title { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public required string Checksum { get; set; }
}
