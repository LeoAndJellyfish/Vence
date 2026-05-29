using Markdig;

namespace Vence.Markdown;

public sealed class MarkdownParser
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownParser()
        : this(new MarkdownPipelineBuilder().UseAdvancedExtensions().Build())
    {
    }

    public MarkdownParser(MarkdownPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public MarkdownDocument Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var syntaxTree = Markdig.Markdown.Parse(markdown, _pipeline);
        return new MarkdownDocument(markdown, syntaxTree);
    }
}
