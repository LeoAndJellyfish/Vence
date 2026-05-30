namespace Vence.Storage;

public interface IWorkspaceStore
{
    Task<IReadOnlyList<WorkspaceDocumentInfo>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(StoredDocument document, CancellationToken cancellationToken = default);

    Task<StoredDocument?> OpenAsync(string path, CancellationToken cancellationToken = default);

    Task<DocumentRecord?> FindRecordByPathAsync(string path, CancellationToken cancellationToken = default);
}
