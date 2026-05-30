using System.Text.Json;
using System.Text.Json.Serialization;
using Vence.Core.Documents;
using Vence.Core.Suggestions;

namespace Vence.AI;

public sealed class SuggestionSchema
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    [JsonPropertyName("suggestions")]
    public List<SuggestionItem> Suggestions { get; set; } = [];

    public static bool TryParse(
        string json,
        Guid documentId,
        int contentLength,
        out IReadOnlyList<Suggestion> suggestions,
        out string? error)
    {
        suggestions = Array.Empty<Suggestion>();
        error = null;

        try
        {
            var schema = JsonSerializer.Deserialize<SuggestionSchema>(json, SerializerOptions);
            if (schema is null)
            {
                error = "AI response was empty.";
                return false;
            }

            var parsedSuggestions = new List<Suggestion>();
            foreach (var item in schema.Suggestions)
            {
                if (!item.TryCreateSuggestion(documentId, contentLength, out var suggestion, out error))
                {
                    suggestions = Array.Empty<Suggestion>();
                    return false;
                }

                if (suggestion is null)
                {
                    error = "AI suggestion could not be created.";
                    suggestions = Array.Empty<Suggestion>();
                    return false;
                }

                parsedSuggestions.Add(suggestion);
            }

            suggestions = parsedSuggestions;
            return true;
        }
        catch (JsonException)
        {
            error = "AI response was not valid JSON.";
            return false;
        }
    }

    public sealed class SuggestionItem
    {
        [JsonPropertyName("range")]
        public RangeItem? Range { get; set; }

        [JsonPropertyName("type")]
        public SuggestionType Type { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("replacement")]
        public string? Replacement { get; set; }

        private bool HasValidType => Enum.IsDefined(Type);

        public bool TryCreateSuggestion(
            Guid documentId,
            int contentLength,
            out Suggestion? suggestion,
            out string? error)
        {
            suggestion = null;
            error = null;

            if (Range is null)
            {
                error = "AI suggestion did not include a range.";
                return false;
            }

            if (Range.Start < 0 || Range.End < Range.Start || Range.End > contentLength)
            {
                error = "AI suggestion range was outside the document.";
                return false;
            }

            if (!HasValidType)
            {
                error = "AI suggestion type was not recognized.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                error = "AI suggestion message was empty.";
                return false;
            }

            suggestion = new Suggestion(
                Guid.NewGuid(),
                documentId,
                new DocumentRange(Range.Start, Range.End),
                Type,
                Message,
                Replacement);

            return true;
        }
    }

    public sealed class RangeItem
    {
        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }
    }
}
