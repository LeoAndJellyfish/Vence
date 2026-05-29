using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Vence.Markdown;

public sealed record MarkdownOutlineItem(int Level, string Title, int Line);

public sealed class OutlineBuilder
{
    public IReadOnlyList<MarkdownOutlineItem> Build(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var headings = new List<MarkdownOutlineItem>();

        foreach (var heading in document.SyntaxTree.Descendants<HeadingBlock>())
        {
            var title = GetPlainText(heading.Inline).Trim();

            if (title.Length == 0)
            {
                continue;
            }

            headings.Add(new MarkdownOutlineItem(heading.Level, title, heading.Line + 1));
        }

        return headings;
    }

    private static string GetPlainText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendInlineText(inline, builder);
        return builder.ToString();
    }

    private static void AppendInlineText(ContainerInline inline, StringBuilder builder)
    {
        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.Text, literal.Content.Start, literal.Content.Length);
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case ContainerInline container:
                    AppendInlineText(container, builder);
                    break;
            }
        }
    }
}
