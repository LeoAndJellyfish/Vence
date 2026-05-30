namespace Vence.AI;

public interface IAssistantService
{
    Task<AssistantResult> GetSuggestionsAsync(
        AssistantRequest request,
        CancellationToken cancellationToken = default);
}
