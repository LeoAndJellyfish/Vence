using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Vence.App.Models;
using Vence.Markdown;
using Vence.Storage;

namespace Vence.App.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private const string DefaultMarkdown = """
        # 欢迎使用 Vence 文思

        点击“打开”选择一个写作文件夹，Vence 会列出其中的 Markdown 文档。

        ## 现在可以做什么

        - 打开一个工作区文件夹
        - 新建 Markdown 文档
        - 在左侧文件树切换文档
        - 使用保存按钮写回磁盘
        """;

    private readonly MarkdownParser _markdownParser = new();
    private readonly OutlineBuilder _outlineBuilder = new();

    private IWorkspaceStore? _workspaceStore;
    private StoredDocument? _currentDocument;
    private string? _workspacePath;
    private string _currentMarkdown = DefaultMarkdown;
    private string _title = "欢迎使用 Vence 文思";
    private string _subtitle = "请选择工作区";
    private string _selectedReaderText = string.Empty;
    private bool _isDirty;
    private bool _isBusy;
    private bool _isReaderMode;

    public ShellViewModel()
    {
        Documents =
        [
            new("尚未打开工作区", "提示", string.Empty, true, true)
        ];

        Folders = [];
        AssistantItems = [];
        ReaderNotes =
        [
            new("ReadingMode", "读者模式", "进入读者模式后会渲染 Markdown，选中文本后可使用 AI 辅助。")
        ];
        OutlineItems = [];
        RefreshDocumentStats();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DocumentListItem> Documents { get; }

    public ObservableCollection<DocumentListItem> Folders { get; }

    public ObservableCollection<AssistantPanelItem> AssistantItems { get; }

    public ObservableCollection<AssistantPanelItem> ReaderNotes { get; }

    public ObservableCollection<OutlineListItem> OutlineItems { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
    }

    public string CurrentMarkdown
    {
        get => _currentMarkdown;
        private set
        {
            if (SetProperty(ref _currentMarkdown, value))
            {
                OnPropertyChanged(nameof(InitialMarkdown));
                RefreshDocumentStats();
            }
        }
    }

    public string InitialMarkdown => CurrentMarkdown;

    public string WordCount { get; private set; } = "字数：0";

    public string CharacterCount { get; private set; } = "字符数：0";

    public string LineCount { get; private set; } = "行数：0";

    public bool HasWorkspace => _workspaceStore is not null;

    public bool CanSave => HasWorkspace && _currentDocument is not null && !_isBusy;

    public bool IsReaderMode
    {
        get => _isReaderMode;
        private set
        {
            if (SetProperty(ref _isReaderMode, value))
            {
                OnPropertyChanged(nameof(ReaderModeLabel));
                OnPropertyChanged(nameof(EditorModeVisibility));
                OnPropertyChanged(nameof(ReaderModeVisibility));
                UpdateSubtitle();
            }
        }
    }

    public string ReaderModeLabel => IsReaderMode ? "退出读者模式" : "读者模式";

    public Visibility EditorModeVisibility => IsReaderMode ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ReaderModeVisibility => IsReaderMode ? Visibility.Visible : Visibility.Collapsed;

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                UpdateSubtitle();
            }
        }
    }

    public async Task OpenWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return;
        }

        await SaveIfDirtyAsync(cancellationToken);

        SetBusy(true);
        try
        {
            _workspacePath = Path.GetFullPath(workspacePath);
            _workspaceStore = new FileSystemWorkspaceStore(_workspacePath);
            OnPropertyChanged(nameof(HasWorkspace));
            OnPropertyChanged(nameof(CanSave));

            await RefreshWorkspaceDocumentsAsync(cancellationToken);

            var firstDocument = Documents.FirstOrDefault(document => !document.IsFolder);
            if (firstDocument is null)
            {
                _currentDocument = null;
                CurrentMarkdown = string.Empty;
                Title = "空工作区";
                IsDirty = false;
                UpdateSubtitle();
                return;
            }

            await SwitchDocumentAsync(firstDocument, cancellationToken, saveCurrent: false);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task CreateDocumentAsync(CancellationToken cancellationToken = default)
    {
        if (_workspaceStore is null)
        {
            return;
        }

        await SaveIfDirtyAsync(cancellationToken);

        var fileName = CreateUntitledFileName();
        var content = """
            # 未命名文档

            从这里开始写。
            """;

        var document = new StoredDocument(fileName, content);
        await _workspaceStore.SaveAsync(document, cancellationToken);
        await RefreshWorkspaceDocumentsAsync(cancellationToken);
        await SwitchDocumentAsync(new DocumentListItem("未命名文档", "文件", fileName), cancellationToken, saveCurrent: false);
    }

    public async Task SwitchDocumentAsync(
        DocumentListItem item,
        CancellationToken cancellationToken = default,
        bool saveCurrent = true)
    {
        if (_workspaceStore is null || item.IsFolder || string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        if (saveCurrent)
        {
            await SaveIfDirtyAsync(cancellationToken);
        }

        SetBusy(true);
        try
        {
            var document = await _workspaceStore.OpenAsync(item.Path, cancellationToken);
            if (document is null)
            {
                return;
            }

            _currentDocument = document;
            CurrentMarkdown = document.Content;
            Title = ExtractDisplayTitle(document);
            IsDirty = false;
            MarkSelectedDocument(document.Path);
            UpdateSubtitle();
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task SaveCurrentDocumentAsync(CancellationToken cancellationToken = default)
    {
        if (_workspaceStore is null || _currentDocument is null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            _currentDocument = _currentDocument with { Content = CurrentMarkdown };
            await _workspaceStore.SaveAsync(_currentDocument, cancellationToken);
            IsDirty = false;
            await RefreshWorkspaceDocumentsAsync(cancellationToken);
            MarkSelectedDocument(_currentDocument.Path);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public void UpdateEditorContent(string markdown)
    {
        if (markdown == CurrentMarkdown)
        {
            return;
        }

        CurrentMarkdown = markdown;
        IsDirty = _currentDocument is not null;
    }

    public void SetReaderMode(bool isReaderMode)
    {
        IsReaderMode = isReaderMode;
        ReaderNotes.Clear();

        if (isReaderMode)
        {
            ReaderNotes.Add(new AssistantPanelItem(
                "ReadingMode",
                "阅读视图已启用",
                "Markdown 已渲染为最终阅读效果。选中任意段落后，可调用左右两侧 AI 辅助。"));
            UpdateReaderSelection(string.Empty);
            return;
        }

        ReaderNotes.Add(new AssistantPanelItem(
            "Edit",
            "编辑模式",
            "返回 Markdown 源文编辑。"));
        UpdateReaderSelection(string.Empty);
    }

    public void UpdateReaderSelection(string selectedText)
    {
        _selectedReaderText = selectedText.Trim();
    }

    public AssistantPanelItem RunReaderAssistant(string action)
    {
        var targetText = string.IsNullOrWhiteSpace(_selectedReaderText)
            ? CurrentMarkdown
            : _selectedReaderText;
        var targetName = string.IsNullOrWhiteSpace(_selectedReaderText) ? "全文" : "选区";

        var item = action switch
        {
            "grammar" => new AssistantPanelItem(
                "ShieldCheck",
                $"语法检查 · {targetName}",
                BuildGrammarSummary(targetText)),
            "polish" => new AssistantPanelItem(
                "Color",
                $"润色建议 · {targetName}",
                BuildPolishSummary(targetText)),
            "completion" => new AssistantPanelItem(
                "Edit",
                $"续写方向 · {targetName}",
                BuildCompletionSummary(targetText)),
            "structure" => new AssistantPanelItem(
                "Document",
                $"结构整理 · {targetName}",
                BuildStructureSummary()),
            _ => new AssistantPanelItem(
                "Help",
                "读者辅助",
                "暂不支持该辅助动作。")
        };

        ReaderNotes.Insert(0, item);
        while (ReaderNotes.Count > 5)
        {
            ReaderNotes.RemoveAt(ReaderNotes.Count - 1);
        }

        return item;
    }

    private async Task RefreshWorkspaceDocumentsAsync(CancellationToken cancellationToken)
    {
        if (_workspaceStore is null)
        {
            return;
        }

        var selectedPath = _currentDocument?.Path;
        var documents = await _workspaceStore.ListDocumentsAsync(cancellationToken);
        var treeItems = BuildDocumentTree(documents, selectedPath);

        Documents.Clear();
        if (treeItems.Count == 0)
        {
            Documents.Add(new DocumentListItem("没有 Markdown 文档", "提示", string.Empty, true, true));
            return;
        }

        foreach (var item in treeItems)
        {
            Documents.Add(item);
        }
    }

    private void RefreshDocumentStats()
    {
        WordCount = $"字数：{CountWords(CurrentMarkdown)}";
        CharacterCount = $"字符数：{CurrentMarkdown.Length}";
        LineCount = $"行数：{CountLines(CurrentMarkdown)}";

        OnPropertyChanged(nameof(WordCount));
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(LineCount));

        OutlineItems.Clear();
        var markdownDocument = _markdownParser.Parse(CurrentMarkdown);
        var outlineIndex = 0;
        foreach (var item in _outlineBuilder.Build(markdownDocument))
        {
            OutlineItems.Add(new OutlineListItem(item.Level, item.Title, outlineIndex));
            outlineIndex++;
        }
    }

    private async Task SaveIfDirtyAsync(CancellationToken cancellationToken)
    {
        if (IsDirty)
        {
            await SaveCurrentDocumentAsync(cancellationToken);
        }
    }

    private void SetBusy(bool isBusy)
    {
        if (SetProperty(ref _isBusy, isBusy, nameof(IsBusy)))
        {
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private bool IsBusy => _isBusy;

    private void UpdateSubtitle()
    {
        if (_currentDocument is null)
        {
            Subtitle = HasWorkspace
                ? "工作区已打开"
                : "请选择工作区";
            return;
        }

        var status = IsDirty ? "未保存" : "已保存";
        var mode = IsReaderMode ? "读者模式" : "编辑模式";
        Subtitle = string.IsNullOrWhiteSpace(_workspacePath)
            ? $"{status} · {mode}"
            : $"{status} · {mode} · {_currentDocument.Path}";
    }

    private void MarkSelectedDocument(string path)
    {
        for (var index = 0; index < Documents.Count; index++)
        {
            var item = Documents[index];
            Documents[index] = item with
            {
                IsSelected = !item.IsFolder && item.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
            };
        }
    }

    private static IReadOnlyList<DocumentListItem> BuildDocumentTree(
        IReadOnlyList<WorkspaceDocumentInfo> documents,
        string? selectedPath)
    {
        var root = new DocumentTreeNode(string.Empty, string.Empty);

        foreach (var document in documents.OrderBy(document => document.Path, StringComparer.OrdinalIgnoreCase))
        {
            var parts = document.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var node = root;
            var currentPath = string.Empty;
            for (var index = 0; index < parts.Length - 1; index++)
            {
                currentPath = string.IsNullOrEmpty(currentPath)
                    ? parts[index]
                    : $"{currentPath}/{parts[index]}";

                if (!node.Folders.TryGetValue(parts[index], out var child))
                {
                    child = new DocumentTreeNode(parts[index], currentPath);
                    node.Folders.Add(parts[index], child);
                }

                node = child;
            }

            node.Files[parts[^1]] = document;
        }

        var items = new List<DocumentListItem>();
        AppendTreeItems(root, depth: 0, selectedPath, items);
        return items;
    }

    private static void AppendTreeItems(
        DocumentTreeNode node,
        int depth,
        string? selectedPath,
        List<DocumentListItem> items)
    {
        foreach (var folder in node.Folders.Values)
        {
            items.Add(new DocumentListItem(folder.Name, "文件夹", folder.Path, false, true, depth));
            AppendTreeItems(folder, depth + 1, selectedPath, items);
        }

        foreach (var document in node.Files.Values)
        {
            items.Add(new DocumentListItem(
                document.Title,
                "文件",
                document.Path,
                document.Path.Equals(selectedPath, StringComparison.OrdinalIgnoreCase),
                false,
                depth));
        }
    }

    private static string ExtractDisplayTitle(StoredDocument document)
    {
        foreach (var line in document.Content.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                return line[2..].Trim();
            }
        }

        return Path.GetFileNameWithoutExtension(document.Path);
    }

    private static string CreateUntitledFileName()
    {
        return $"未命名-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.md";
    }

    private static int CountLines(string markdown)
    {
        if (markdown.Length == 0)
        {
            return 0;
        }

        return markdown.Count(character => character == '\n') + 1;
    }

    private static int CountWords(string markdown)
    {
        var count = 0;
        var inToken = false;

        foreach (var character in markdown)
        {
            if (char.IsWhiteSpace(character) || char.IsPunctuation(character) || char.IsSymbol(character))
            {
                inToken = false;
                continue;
            }

            if (IsCjk(character))
            {
                count++;
                inToken = false;
                continue;
            }

            if (!inToken)
            {
                count++;
                inToken = true;
            }
        }

        return count;
    }

    private static bool IsCjk(char character)
    {
        return character is >= '\u3400' and <= '\u9fff';
    }

    private static string BuildGrammarSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "当前没有可分析的文本。";
        }

        var sentenceCount = text.Count(character => character is '。' or '！' or '？' or '.' or '!' or '?');
        return sentenceCount == 0
            ? "文本较短，建议补足完整句子后再检查语法和标点。"
            : $"已覆盖约 {sentenceCount} 个句子。建议重点检查重复用词、长句断句和中英文标点一致性。";
    }

    private static string BuildPolishSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "当前没有可润色的文本。";
        }

        return text.Length > 180
            ? "这段信息密度较高，可拆成 2-3 个自然段，并把关键判断提前。"
            : "这段适合做轻量润色：保留原意，增强动词，减少抽象形容词。";
    }

    private static string BuildCompletionSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "可以先选中一段文字，我会围绕它给出续写方向。";
        }

        return "可从三个方向续写：补充一个具体例子、解释背后的原因、或用一句收束性判断连接下一节。";
    }

    private string BuildStructureSummary()
    {
        var headingCount = OutlineItems.Count;
        return headingCount == 0
            ? "当前没有识别到标题。建议至少加入一级标题和 2-3 个二级标题。"
            : $"已识别 {headingCount} 个标题。可检查层级是否连续，避免 H1 后直接跳到 H3。";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class DocumentTreeNode
    {
        public DocumentTreeNode(string name, string path)
        {
            Name = name;
            Path = path;
            Folders = new SortedDictionary<string, DocumentTreeNode>(StringComparer.OrdinalIgnoreCase);
            Files = new SortedDictionary<string, WorkspaceDocumentInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public string Path { get; }

        public SortedDictionary<string, DocumentTreeNode> Folders { get; }

        public SortedDictionary<string, WorkspaceDocumentInfo> Files { get; }
    }
}
