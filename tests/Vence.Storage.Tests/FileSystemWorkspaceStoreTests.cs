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

    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
    }
}
