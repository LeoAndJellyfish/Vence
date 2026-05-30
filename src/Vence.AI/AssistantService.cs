using Microsoft.Extensions.AI;
using Vence.AI.Prompts;

namespace Vence.AI;

public sealed class AssistantService : IAssistantService
{
    private const string InvalidResponseMessage = "AI 返回了无法识别的建议格式，请稍后重试。";
    private const string ProviderFailureMessage = "AI 服务暂时不可用，已保留当前文档内容。";

    private readonly IChatClientFactory _chatClientFactory;

    public AssistantService(IChatClientFactory chatClientFactory)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
    }

    public async Task<AssistantResult> GetSuggestionsAsync(
        AssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var chatClient = _chatClientFactory.CreateClient();
            var response = await chatClient.GetResponseAsync(
                CreateMessages(request),
                new ChatOptions
                {
                    Temperature = 0.2f
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                return AssistantResult.Failure("invalid_response", InvalidResponseMessage);
            }

            return SuggestionSchema.TryParse(
                response.Text,
                request.Document.Id,
                request.Document.Content.Length,
                out var suggestions,
                out _)
                ? AssistantResult.Success(suggestions)
                : AssistantResult.Failure("invalid_response", InvalidResponseMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return AssistantResult.Failure("provider_failure", ProviderFailureMessage);
        }
    }

    private static ChatMessage[] CreateMessages(AssistantRequest request)
    {
        return
        [
            new(ChatRole.System, GetSystemPrompt(request.Mode)),
            new(ChatRole.User, CreateUserPrompt(request))
        ];
    }

    private static string GetSystemPrompt(AssistantMode mode)
    {
        return mode switch
        {
            AssistantMode.GrammarCheck => GrammarCheckPrompt.SystemPrompt,
            AssistantMode.Rewrite => RewritePrompt.SystemPrompt,
            AssistantMode.ReaderMode => ReaderModePrompt.SystemPrompt,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported assistant mode.")
        };
    }

    private static string CreateUserPrompt(AssistantRequest request)
    {
        return $$"""
            请阅读以下 Markdown 文本，并仅返回符合此 schema 的 JSON：
            {
              "suggestions": [
                {
                  "range": { "start": 0, "end": 0 },
                  "type": "Grammar | Rewrite | Reader | Format | Completion",
                  "message": "给用户看的简短建议",
                  "replacement": "可选。没有直接替换文本时为 null"
                }
              ]
            }

            约束：
            - range 使用原文中的 UTF-16 字符索引，end 为开区间。
            - 不要自动改写原文。
            - 不确定时返回空数组。

            Markdown:
            {{request.TextForModel}}
            """;
    }
}
