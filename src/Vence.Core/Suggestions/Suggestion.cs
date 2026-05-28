using Vence.Core.Documents;

namespace Vence.Core.Suggestions;

public sealed class Suggestion
{
    public Suggestion(
        Guid id,
        Guid documentId,
        DocumentRange range,
        SuggestionType type,
        string message,
        string? replacement)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Suggestion id cannot be empty.", nameof(id)) : id;
        DocumentId = documentId == Guid.Empty ? throw new ArgumentException("Document id cannot be empty.", nameof(documentId)) : documentId;
        Range = range ?? throw new ArgumentNullException(nameof(range));
        Type = type;
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Message is required.", nameof(message)) : message;
        Replacement = replacement;
    }

    public Guid Id { get; }

    public Guid DocumentId { get; }

    public DocumentRange Range { get; }

    public SuggestionType Type { get; }

    public string Message { get; }

    public string? Replacement { get; }

    public SuggestionStatus Status { get; private set; } = SuggestionStatus.Pending;

    public void Accept()
    {
        Status = SuggestionStatus.Accepted;
    }

    public void Reject()
    {
        Status = SuggestionStatus.Rejected;
    }
}
