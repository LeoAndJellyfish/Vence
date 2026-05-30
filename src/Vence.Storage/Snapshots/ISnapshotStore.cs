namespace Vence.Storage.Snapshots;

public interface ISnapshotStore
{
    Task<SnapshotRecord> CreateSnapshotAsync(
        StoredDocument document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnapshotRecord>> ListSnapshotsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<StoredSnapshot?> OpenSnapshotAsync(
        Guid snapshotId,
        CancellationToken cancellationToken = default);
}
