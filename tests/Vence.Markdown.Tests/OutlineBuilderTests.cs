using Vence.Markdown;

namespace Vence.Markdown.Tests;

public sealed class OutlineBuilderTests
{
    [Fact]
    public void BuildReturnsH1AndH2HeadingsInDocumentOrder()
    {
        const string markdown = """
            # 标题
            ## 历史与城市风貌
            ## 艺术与文化影响
            """;

        var document = new MarkdownParser().Parse(markdown);
        var outline = new OutlineBuilder().Build(document);

        Assert.Collection(
            outline,
            item =>
            {
                Assert.Equal(1, item.Level);
                Assert.Equal("标题", item.Title);
            },
            item =>
            {
                Assert.Equal(2, item.Level);
                Assert.Equal("历史与城市风貌", item.Title);
            },
            item =>
            {
                Assert.Equal(2, item.Level);
                Assert.Equal("艺术与文化影响", item.Title);
            });
    }

    [Fact]
    public void BuildKeepsInlineFormattingAsReadableText()
    {
        const string markdown = """
            # 写作中的 **Vence**
            ## 使用 `Markdown` 捕捉灵感
            """;

        var document = new MarkdownParser().Parse(markdown);
        var outline = new OutlineBuilder().Build(document);

        Assert.Equal("写作中的 Vence", outline[0].Title);
        Assert.Equal("使用 Markdown 捕捉灵感", outline[1].Title);
    }
}
