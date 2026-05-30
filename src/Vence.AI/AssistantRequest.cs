using Vence.Core.Documents;

namespace Vence.AI;

public sealed class AssistantRequest
{
    public AssistantRequest(Document document, AssistantMode mode, string? selectedText = null)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Mode = mode;
        SelectedText = selectedText;
    }

    public Document Document { get; }

    public AssistantMode Mode { get; }

    public string? SelectedText { get; }

    public string TextForModel => string.IsNullOrWhiteSpace(SelectedText)
        ? Document.Content
        : SelectedText;
}
