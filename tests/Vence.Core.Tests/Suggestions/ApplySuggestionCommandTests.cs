using Vence.Core.Commands;
using Vence.Core.Documents;
using Vence.Core.Suggestions;

namespace Vence.Core.Tests.Suggestions;

public sealed class ApplySuggestionCommandTests
{
    [Fact]
    public void CreatingSuggestionDoesNotChangeDocumentContent()
    {
        var documentId = Guid.NewGuid();
        var document = new Document(documentId, "vence.md", "Vence 是一款编辑器");

        _ = new Suggestion(
            Guid.NewGuid(),
            documentId,
            new DocumentRange(9, 12),
            SuggestionType.Rewrite,
            "让表达更贴近产品定位。",
            "智能写作空间");

        Assert.Equal("Vence 是一款编辑器", document.Content);
    }

    [Fact]
    public void ExecuteAppliesPendingSuggestionReplacement()
    {
        var documentId = Guid.NewGuid();
        var document = new Document(documentId, "vence.md", "Vence 是一款编辑器");
        var suggestion = new Suggestion(
            Guid.NewGuid(),
            documentId,
            new DocumentRange(9, 12),
            SuggestionType.Rewrite,
            "让表达更贴近产品定位。",
            "智能写作空间");

        var command = new ApplySuggestionCommand(document, suggestion);

        var result = command.Execute();

        Assert.Same(document, result);
        Assert.Equal("Vence 是一款智能写作空间", document.Content);
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
    }

    [Fact]
    public void ExecuteRejectsSuggestionForDifferentDocument()
    {
        var document = new Document(Guid.NewGuid(), "vence.md", "保持你的声音");
        var suggestion = new Suggestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DocumentRange(0, 2),
            SuggestionType.Rewrite,
            "测试跨文档保护。",
            "保留");

        var command = new ApplySuggestionCommand(document, suggestion);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
        Assert.Equal("保持你的声音", document.Content);
    }
}
