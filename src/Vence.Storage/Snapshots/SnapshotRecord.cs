namespace Vence.Storage.Snapshots;

public sealed record SnapshotRecord(
    Guid Id,
    Guid DocumentId,
    string DocumentPath,
    DateTimeOffset CreatedAt,
    string Checksum,
    long SizeBytes);
