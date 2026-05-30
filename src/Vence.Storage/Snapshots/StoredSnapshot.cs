namespace Vence.Storage.Snapshots;

public sealed record StoredSnapshot(
    Guid Id,
    Guid DocumentId,
    string DocumentPath,
    DateTimeOffset CreatedAt,
    string Content,
    string Checksum);
