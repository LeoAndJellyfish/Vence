using System.Collections.ObjectModel;
using Vence.App.Models;
using Vence.Markdown;

namespace Vence.App.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel()
    {
        Documents =
        [
            new("探索山城 Vence 的艺术与生活之美", "最近", true),
            new("普罗旺斯的阳光", "最近"),
            new("读《人类简史》有感", "最近"),
            new("会议纪要 0528", "最近")
        ];

        Folders =
        [
            new("旅行随笔", "文件夹"),
            new("读书笔记", "文件夹"),
            new("项目资料", "文件夹"),
            new("创作灵感", "文件夹")
        ];

        AssistantItems =
        [
            new("ShieldCheck", "智能语法检查", "未发现明显语法错误"),
            new("Edit", "智能补全", "我会这么写……"),
            new("Color", "智能润色", "你应该想说的是……"),
            new("Document", "智能格式调整", "当前场景：旅行文章")
        ];

        ReaderNotes =
        [
            new("Help", "这里好像和前面矛盾？", "第 5 段"),
            new("Important", "这里逻辑不通，能否再展开说明？", "第 8 段"),
            new("Like", "这里写得好！", "第 12 段")
        ];

        var markdownDocument = new MarkdownParser().Parse(InitialMarkdown);
        OutlineItems = new ObservableCollection<OutlineListItem>(
            new OutlineBuilder()
                .Build(markdownDocument)
                .Select(item => new OutlineListItem(item.Level, item.Title)));
    }

    public ObservableCollection<DocumentListItem> Documents { get; }

    public ObservableCollection<DocumentListItem> Folders { get; }

    public ObservableCollection<AssistantPanelItem> AssistantItems { get; }

    public ObservableCollection<AssistantPanelItem> ReaderNotes { get; }

    public ObservableCollection<OutlineListItem> OutlineItems { get; }

    public string Title => "探索山城 Vence 的艺术与生活之美";

    public string Subtitle => "已保存";

    public string InitialMarkdown => """
        # 探索山城 Vence 的艺术与生活之美

        Vence，这座位于法国蔚蓝海岸腹地的历史山城，以其深厚的艺术底蕴与宁静的生活氛围吸引着无数旅人。

        ## 历史与城市风貌

        Vence 的历史可追溯至古罗马时期，至今仍保留着环绕老城的石墙、狭窄街巷与古老喷泉。清晨的集市从城门一路铺开，橄榄、鲜花与手作陶器把街角装点得温柔而生动。

        ## 写作方向

        - 以步行路线串联老城、礼拜堂与观景台
        - 保留个人观察，不写成普通旅行清单
        - 在结尾加入一段关于慢生活的反思
        """;

    public string WordCount => "字数：1258";

    public string CharacterCount => "字符数：2217";

    public string LineCount => "行数：68";
}
