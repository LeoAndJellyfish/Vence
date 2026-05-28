namespace Vence.Storage;

public sealed record DocumentRecord(
    Guid Id,
    string Path,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Checksum);
