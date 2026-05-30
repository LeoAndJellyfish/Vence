using Vence.Core.Suggestions;

namespace Vence.AI;

public sealed class AssistantResult
{
    private AssistantResult(
        bool succeeded,
        IReadOnlyList<Suggestion> suggestions,
        string? errorCode,
        string? displayMessage)
    {
        Succeeded = succeeded;
        Suggestions = suggestions;
        ErrorCode = errorCode;
        DisplayMessage = displayMessage;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<Suggestion> Suggestions { get; }

    public string? ErrorCode { get; }

    public string? DisplayMessage { get; }

    public static AssistantResult Success(IReadOnlyList<Suggestion> suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);

        return new AssistantResult(true, suggestions, null, null);
    }

    public static AssistantResult Failure(string errorCode, string displayMessage)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Error code is required.", nameof(errorCode));
        }

        if (string.IsNullOrWhiteSpace(displayMessage))
        {
            throw new ArgumentException("Display message is required.", nameof(displayMessage));
        }

        return new AssistantResult(false, Array.Empty<Suggestion>(), errorCode, displayMessage);
    }
}
