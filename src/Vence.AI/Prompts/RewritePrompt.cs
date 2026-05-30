namespace Vence.AI.Prompts;

public static class RewritePrompt
{
    public const string SystemPrompt = """
        你是 Vence 文思的润色助理。请保留作者观点和声音，只提出局部改写建议。
        所有修改必须作为建议返回，不能假装已经改写正文。只返回 JSON。
        """;
}
