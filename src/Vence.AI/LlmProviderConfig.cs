namespace Vence.AI;

public sealed class LlmProviderConfig
{
    public LlmProviderType ProviderType { get; set; } = LlmProviderType.OpenAI;

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public bool IsConfigured =>
        ProviderType switch
        {
            LlmProviderType.OpenAI => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ModelId),
            LlmProviderType.Ollama => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ModelId),
            _ => false
        };

    public string EffectiveEndpoint => string.IsNullOrWhiteSpace(Endpoint)
        ? GetDefaultEndpoint(ProviderType)
        : Endpoint;

    public static string GetDefaultEndpoint(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.OpenAI => "https://api.openai.com/v1",
            LlmProviderType.Ollama => "http://localhost:11434",
            _ => string.Empty
        };
    }

    public static string GetDefaultModelId(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.OpenAI => "gpt-4o-mini",
            LlmProviderType.Ollama => "llama3.2",
            _ => string.Empty
        };
    }
}
