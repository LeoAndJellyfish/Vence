using System.Text.Json;

namespace Vence.EditorHost;

public sealed record EditorMessage(int Version, string Type, string RequestId, JsonElement Payload)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static EditorMessage Create(string type, object? payload = null, string? requestId = null)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Message type is required.", nameof(type));
        }

        return new EditorMessage(
            CurrentVersion,
            type,
            requestId ?? Guid.NewGuid().ToString("N"),
            JsonSerializer.SerializeToElement(payload ?? new { }, SerializerOptions));
    }

    public static bool TryParse(string json, out EditorMessage? message)
    {
        try
        {
            message = JsonSerializer.Deserialize<EditorMessage>(json, SerializerOptions);
            return message is not null &&
                message.Version == CurrentVersion &&
                !string.IsNullOrWhiteSpace(message.Type) &&
                !string.IsNullOrWhiteSpace(message.RequestId);
        }
        catch (JsonException)
        {
            message = null;
            return false;
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }
}
