namespace Vence.Markdown;

public sealed class MarkdownDocument
{
    internal MarkdownDocument(string source, Markdig.Syntax.MarkdownDocument syntaxTree)
    {
        Source = source;
        SyntaxTree = syntaxTree;
    }

    public string Source { get; }

    internal Markdig.Syntax.MarkdownDocument SyntaxTree { get; }
}
