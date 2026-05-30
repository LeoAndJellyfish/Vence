using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vence.Storage.Snapshots;

public sealed class SnapshotStore : ISnapshotStore
{
    private const string MetadataDirectoryName = ".vence";
    private const string SnapshotsDirectoryName = "snapshots";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly string _snapshotsPath;

    public SnapshotStore(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Workspace root path is required.", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        _snapshotsPath = Path.Combine(_rootPath, MetadataDirectoryName, SnapshotsDirectoryName);
        Directory.CreateDirectory(_snapshotsPath);
    }

    public async Task<SnapshotRecord> CreateSnapshotAsync(
        StoredDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var relativePath = NormalizeRelativePath(document.Path);
        var snapshotId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var documentSnapshotPath = GetDocumentSnapshotPath(document.Id);
        Directory.CreateDirectory(documentSnapshotPath);

        var filePrefix = $"{createdAt.UtcTicks:D19}_{snapshotId:N}";
        var contentFileName = $"{filePrefix}.md";
        var metadataFileName = $"{filePrefix}.json";
        var contentPath = Path.Combine(documentSnapshotPath, contentFileName);
        var metadataPath = Path.Combine(documentSnapshotPath, metadataFileName);

        await File.WriteAllTextAsync(contentPath, document.Content, Encoding.UTF8, cancellationToken);

        var metadata = new SnapshotMetadata
        {
            Id = snapshotId,
            DocumentId = document.Id,
            DocumentPath = relativePath,
            CreatedAt = createdAt,
            ContentFileName = contentFileName,
            Checksum = ComputeSha256(document.Content),
            SizeBytes = new FileInfo(contentPath).Length
        };

        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(metadata, SerializerOptions),
            Encoding.UTF8,
            cancellationToken);

        return metadata.ToRecord();
    }

    public async Task<IReadOnlyList<SnapshotRecord>> ListSnapshotsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id cannot be empty.", nameof(documentId));
        }

        var documentSnapshotPath = GetDocumentSnapshotPath(documentId);
        if (!Directory.Exists(documentSnapshotPath))
        {
            return Array.Empty<SnapshotRecord>();
        }

        var snapshots = new List<SnapshotRecord>();
        foreach (var metadataPath in Directory.EnumerateFiles(documentSnapshotPath, "*.json"))
        {
            var metadata = await ReadMetadataAsync(metadataPath, cancellationToken);
            if (metadata is not null)
            {
                snapshots.Add(metadata.ToRecord());
            }
        }

        return snapshots
            .OrderByDescending(item => item.CreatedAt)
            .ToArray();
    }

    public async Task<StoredSnapshot?> OpenSnapshotAsync(
        Guid snapshotId,
        CancellationToken cancellationToken = default)
    {
        if (snapshotId == Guid.Empty)
        {
            throw new ArgumentException("Snapshot id cannot be empty.", nameof(snapshotId));
        }

        foreach (var metadataPath in Directory.EnumerateFiles(_snapshotsPath, "*.json", SearchOption.AllDirectories))
        {
            var metadata = await ReadMetadataAsync(metadataPath, cancellationToken);
            if (metadata?.Id != snapshotId)
            {
                continue;
            }

            var contentPath = Path.Combine(Path.GetDirectoryName(metadataPath)!, metadata.ContentFileName);
            if (!File.Exists(contentPath))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(contentPath, Encoding.UTF8, cancellationToken);
            return new StoredSnapshot(
                metadata.Id,
                metadata.DocumentId,
                metadata.DocumentPath,
                metadata.CreatedAt,
                content,
                metadata.Checksum);
        }

        return null;
    }

    private string GetDocumentSnapshotPath(Guid documentId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id cannot be empty.", nameof(documentId));
        }

        return Path.Combine(_snapshotsPath, documentId.ToString("N"));
    }

    private static async Task<SnapshotMetadata?> ReadMetadataAsync(
        string metadataPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(metadataPath);
        return await JsonSerializer.DeserializeAsync<SnapshotMetadata>(
            stream,
            SerializerOptions,
            cancellationToken);
    }

    private static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Document path is required.", nameof(path));
        }

        if (Path.IsPathRooted(path))
        {
            throw new InvalidOperationException("Document path must be relative to the workspace.");
        }

        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class SnapshotMetadata
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public required string DocumentPath { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public required string ContentFileName { get; set; }

        public required string Checksum { get; set; }

        public long SizeBytes { get; set; }

        public SnapshotRecord ToRecord()
        {
            return new SnapshotRecord(
                Id,
                DocumentId,
                DocumentPath,
                CreatedAt,
                Checksum,
                SizeBytes);
        }
    }
}
