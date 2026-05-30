using Vence.Core.Documents;

namespace Vence.Core.Commands;

public sealed class RestoreSnapshotCommand : ICommand<Document>
{
    private readonly Document _document;
    private readonly string _snapshotContent;

    public RestoreSnapshotCommand(Document document, string snapshotContent)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _snapshotContent = snapshotContent ?? throw new ArgumentNullException(nameof(snapshotContent));
    }

    public Document Execute()
    {
        _document.Replace(new DocumentRange(0, _document.Content.Length), _snapshotContent);
        return _document;
    }
}
