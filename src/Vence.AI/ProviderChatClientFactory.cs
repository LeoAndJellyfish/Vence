using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace Vence.AI;

public sealed class ProviderChatClientFactory : IChatClientFactory
{
    private volatile LlmProviderConfig _config;

    public ProviderChatClientFactory(LlmProviderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public LlmProviderConfig CurrentConfig => _config;

    public void UpdateConfig(LlmProviderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public IChatClient CreateClient()
    {
        var config = _config;

        return config.ProviderType switch
        {
            LlmProviderType.OpenAI => CreateOpenAIClient(config),
            LlmProviderType.Ollama => CreateOllamaClient(config),
            _ => throw new InvalidOperationException($"Unsupported provider type: {config.ProviderType}")
        };
    }

    private static IChatClient CreateOpenAIClient(LlmProviderConfig config)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = string.IsNullOrWhiteSpace(config.Endpoint)
                ? null
                : new Uri(config.Endpoint)
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential(config.ApiKey), clientOptions);
        var chatClient = openAIClient.GetChatClient(config.ModelId);

        return chatClient.AsIChatClient();
    }

    private static IChatClient CreateOllamaClient(LlmProviderConfig config)
    {
        var endpoint = config.EffectiveEndpoint;
        return new OllamaChatClient(new Uri(endpoint), config.ModelId);
    }
}
