using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vence.Storage.Entities;

namespace Vence.Storage;

public sealed class FileSystemWorkspaceStore : IWorkspaceStore
{
    private const string MetadataDirectoryName = ".vence";
    private const string DatabaseFileName = "workspace.db";

    private readonly string _rootPath;
    private readonly DbContextOptions<VenceDbContext> _dbContextOptions;

    public FileSystemWorkspaceStore(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Workspace root path is required.", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);

        var metadataPath = Path.Combine(_rootPath, MetadataDirectoryName);
        Directory.CreateDirectory(metadataPath);

        var dbPath = Path.Combine(metadataPath, DatabaseFileName);
        _dbContextOptions = new DbContextOptionsBuilder<VenceDbContext>()
            .UseSqlite($"Data Source={dbPath};Pooling=False")
            .Options;
    }

    public async Task SaveAsync(StoredDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(document.Path))
        {
            throw new InvalidOperationException("Document path is required before saving.");
        }

        var relativePath = NormalizeRelativePath(document.Path);
        var filePath = GetWorkspaceFilePath(relativePath);
        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, document.Content, Encoding.UTF8, cancellationToken);
        File.Move(tempPath, filePath, overwrite: true);

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var checksum = ComputeSha256(document.Content);
        var now = DateTimeOffset.UtcNow;
        var entity = await dbContext.Documents.SingleOrDefaultAsync(item => item.Path == relativePath, cancellationToken);

        if (entity is null)
        {
            entity = new DocumentEntity
            {
                Id = document.Id,
                Path = relativePath,
                Title = GetTitle(relativePath),
                CreatedAt = now,
                UpdatedAt = now,
                Checksum = checksum
            };

            dbContext.Documents.Add(entity);
        }
        else
        {
            entity.Title = GetTitle(relativePath);
            entity.UpdatedAt = now;
            entity.Checksum = checksum;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredDocument?> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizeRelativePath(path);
        var filePath = GetWorkspaceFilePath(relativePath);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
        var record = await FindRecordByPathAsync(relativePath, cancellationToken);
        var documentId = record?.Id ?? Guid.NewGuid();

        return new StoredDocument(documentId, relativePath, content);
    }

    public async Task<DocumentRecord?> FindRecordByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizeRelativePath(path);

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var entity = await dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Path == relativePath, cancellationToken);

        return entity is null
            ? null
            : new DocumentRecord(
                entity.Id,
                entity.Path,
                entity.Title,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.Checksum);
    }

    private VenceDbContext CreateDbContext()
    {
        return new VenceDbContext(_dbContextOptions);
    }

    private string GetWorkspaceFilePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var relativeToRoot = Path.GetRelativePath(_rootPath, fullPath);

        if (relativeToRoot == "." ||
            relativeToRoot == ".." ||
            relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relativeToRoot.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativeToRoot))
        {
            throw new InvalidOperationException("Document path must stay inside the workspace.");
        }

        return fullPath;
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

    private static string GetTitle(string relativePath)
    {
        return Path.GetFileNameWithoutExtension(relativePath);
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
