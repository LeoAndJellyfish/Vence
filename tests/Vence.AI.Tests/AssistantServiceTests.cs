using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Vence.Core.Documents;
using Vence.Core.Suggestions;

namespace Vence.AI.Tests;

public sealed class AssistantServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsyncReturnsStructuredSuggestion()
    {
        var document = new Document(Guid.NewGuid(), "vence.md", "Vence 是一款编辑器");
        var service = new AssistantService(new StubChatClientFactory(new StubChatClient("""
            {
              "suggestions": [
                {
                  "range": { "start": 9, "end": 12 },
                  "type": "Rewrite",
                  "message": "让表达更贴近产品定位。",
                  "replacement": "智能写作空间"
                }
              ]
            }
            """)));

        var result = await service.GetSuggestionsAsync(
            new AssistantRequest(document, AssistantMode.Rewrite));

        Assert.True(result.Succeeded);
        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal(document.Id, suggestion.DocumentId);
        Assert.Equal(SuggestionType.Rewrite, suggestion.Type);
        Assert.Equal(new DocumentRange(9, 12), suggestion.Range);
        Assert.Equal("智能写作空间", suggestion.Replacement);
        Assert.Equal("Vence 是一款编辑器", document.Content);
    }

    [Fact]
    public async Task GetSuggestionsAsyncRejectsInvalidJsonWithoutChangingDocument()
    {
        var document = new Document(Guid.NewGuid(), "vence.md", "保持你的声音");
        var service = new AssistantService(new StubChatClientFactory(new StubChatClient("不是 JSON")));

        var result = await service.GetSuggestionsAsync(
            new AssistantRequest(document, AssistantMode.GrammarCheck));

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.ErrorCode);
        Assert.Empty(result.Suggestions);
        Assert.Equal("保持你的声音", document.Content);
    }

    [Fact]
    public async Task GetSuggestionsAsyncReturnsDisplayableErrorWhenProviderFails()
    {
        var document = new Document(Guid.NewGuid(), "vence.md", "Vence 是一款编辑器");
        var service = new AssistantService(
            new StubChatClientFactory(new StubChatClient(providerError: new InvalidOperationException("provider down"))));

        var result = await service.GetSuggestionsAsync(
            new AssistantRequest(document, AssistantMode.ReaderMode));

        Assert.False(result.Succeeded);
        Assert.Equal("provider_failure", result.ErrorCode);
        Assert.Contains("AI 服务暂时不可用", result.DisplayMessage);
        Assert.Empty(result.Suggestions);
        Assert.Equal("Vence 是一款编辑器", document.Content);
    }

    private sealed class StubChatClientFactory : IChatClientFactory
    {
        private readonly IChatClient _chatClient;

        public StubChatClientFactory(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public IChatClient CreateClient()
        {
            return _chatClient;
        }
    }

    private sealed class StubChatClient : IChatClient
    {
        private readonly string? _responseText;
        private readonly Exception? _providerError;

        public StubChatClient(string? responseText = null, Exception? providerError = null)
        {
            _responseText = responseText;
            _providerError = providerError;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (_providerError is not null)
            {
                throw _providerError;
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responseText ?? "")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
