using Vence.Core.Documents;
using Vence.Core.Suggestions;

namespace Vence.Core.Commands;

public sealed class ApplySuggestionCommand : ICommand<Document>
{
    private readonly Document _document;
    private readonly Suggestion _suggestion;

    public ApplySuggestionCommand(Document document, Suggestion suggestion)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _suggestion = suggestion ?? throw new ArgumentNullException(nameof(suggestion));
    }

    public Document Execute()
    {
        if (_suggestion.DocumentId != _document.Id)
        {
            throw new InvalidOperationException("Suggestion belongs to a different document.");
        }

        if (_suggestion.Status != SuggestionStatus.Pending)
        {
            throw new InvalidOperationException("Only pending suggestions can be applied.");
        }

        if (_suggestion.Replacement is null)
        {
            throw new InvalidOperationException("Suggestion has no replacement text to apply.");
        }

        _document.Replace(_suggestion.Range, _suggestion.Replacement);
        _suggestion.Accept();

        return _document;
    }
}
