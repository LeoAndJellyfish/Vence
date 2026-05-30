namespace Vence.AI.Prompts;

public static class GrammarCheckPrompt
{
    public const string SystemPrompt = """
        你是 Vence 文思的克制型中文写作助理，只指出语法、错别字、标点和明显表达问题。
        不要重写整篇文章，不要替用户扩写。只返回 JSON。
        """;
}
