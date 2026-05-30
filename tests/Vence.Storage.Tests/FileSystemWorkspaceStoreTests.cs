using Vence.Core.Commands;
using Vence.Core.Documents;
using Vence.Storage.Snapshots;

namespace Vence.Storage.Tests;

public sealed class FileSystemWorkspaceStoreTests : IDisposable
{
    private readonly string _workspacePath = Path.Combine(
        Path.GetTempPath(),
        "Vence.Storage.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsyncWritesMarkdownFileAndMetadata()
    {
        var store = new FileSystemWorkspaceStore(_workspacePath);
        var documentId = Guid.NewGuid();
        var document = new StoredDocument(documentId, "notes/vence.md", "# Vence 文思\n\n以文启思");

        await store.SaveAsync(document);

        var filePath = Path.Combine(_workspacePath, "notes", "vence.md");
        var record = await store.FindRecordByPathAsync("notes/vence.md");

        Assert.True(File.Exists(filePath));
        Assert.Equal("# Vence 文思\n\n以文启思", await File.ReadAllTextAsync(filePath));
        Assert.NotNull(record);
        Assert.Equal(documentId, record.Id);
        Assert.Equal("notes/vence.md", record.Path);
        Assert.Equal("vence", record.Title);
        Assert.NotEqual(string.Empty, record.Checksum);
    }

    [Fact]
    public async Task OpenAsyncReturnsSavedMarkdownDocument()
    {
        var store = new FileSystemWorkspaceStore(_workspacePath);
        var document = new StoredDocument(Guid.NewGuid(), "draft.md", "保持你的声音。");

        await store.SaveAsync(document);

        var loaded = await store.OpenAsync("draft.md");

        Assert.NotNull(loaded);
        Assert.Equal(document.Id, loaded.Id);
        Assert.Equal("draft.md", loaded.Path);
        Assert.Equal("保持你的声音。", loaded.Content);
    }

    [Fact]
    public async Task SaveAsyncPreservesMarkdownLineBreaks()
    {
        var store = new FileSystemWorkspaceStore(_workspacePath);
        var content = "# 标题\n\n第一段\n第二段\n\n- 条目 A\n- 条目 B\n";

        await store.SaveAsync(new StoredDocument(Guid.NewGuid(), "line-breaks.md", content));

        var loaded = await store.OpenAsync("line-breaks.md");

        Assert.NotNull(loaded);
        Assert.Equal(content, loaded.Content);
        Assert.Equal(content, await File.ReadAllTextAsync(Path.Combine(_workspacePath, "line-breaks.md")));
    }

    [Fact]
    public async Task ListDocumentsAsyncReturnsMarkdownFilesOutsideMetadataDirectory()
    {
        var store = new FileSystemWorkspaceStore(_workspacePath);

        Directory.CreateDirectory(Path.Combine(_workspacePath, "notes"));
        Directory.CreateDirectory(Path.Combine(_workspacePath, ".vence"));

        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "draft.md"), "# 草稿");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "notes", "reading.markdown"), "# 阅读");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, "notes", "ignored.txt"), "不是文档");
        await File.WriteAllTextAsync(Path.Combine(_workspacePath, ".vence", "snapshot.md"), "元数据");

        var documents = await store.ListDocumentsAsync();

        Assert.Equal(2, documents.Count);
        Assert.Contains(documents, document => document.Path == "draft.md" && document.Title == "draft");
        Assert.Contains(documents, document => document.Path == "notes/reading.markdown" && document.Title == "reading");
        Assert.DoesNotContain(documents, document => document.Path.StartsWith(".vence/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAsyncCreatesSnapshotsThatCanRestorePreviousVersion()
    {
        var store = new FileSystemWorkspaceStore(_workspacePath);
        var snapshotStore = new SnapshotStore(_workspacePath);
        var documentId = Guid.NewGuid();

        await store.SaveAsync(new StoredDocument(documentId, "draft.md", "第一版"));
        await store.SaveAsync(new StoredDocument(documentId, "draft.md", "第二版"));

        var snapshots = await snapshotStore.ListSnapshotsAsync(documentId);
        Assert.Equal(2, snapshots.Count);

        StoredSnapshot? previousSnapshot = null;
        foreach (var snapshot in snapshots)
        {
            var storedSnapshot = await snapshotStore.OpenSnapshotAsync(snapshot.Id);
            if (storedSnapshot?.Content == "第一版")
            {
                previousSnapshot = storedSnapshot;
                break;
            }
        }

        Assert.NotNull(previousSnapshot);

        var document = new Document(documentId, "draft.md", "第二版");
        var command = new RestoreSnapshotCommand(document, previousSnapshot.Content);

        command.Execute();

        Assert.Equal("第一版", document.Content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
    }
}
