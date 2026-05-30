using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vence.App.Models;
using Vence.App.ViewModels;
using Vence.EditorHost;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Vence.App.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView()
    {
        ViewModel = new ShellViewModel();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public ShellViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        MarkdownEditorHost.EditorMessageReceived += OnEditorMessageReceived;
        SyncSelection();
    }

    private async void OnOpenWorkspaceClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = await PickWorkspaceFolderAsync();
            if (folder is null)
            {
                return;
            }

            await ViewModel.OpenWorkspaceAsync(folder.Path);
            SyncEditorFromViewModel();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("打开工作区失败", ex.Message);
        }
    }

    private async void OnNewDocumentClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.CreateDocumentAsync();
            SyncEditorFromViewModel();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("新建文档失败", ex.Message);
        }
    }

    private async void OnSaveDocumentClick(object sender, RoutedEventArgs e)
    {
        await SaveCurrentDocumentAsync();
    }

    private async Task SaveCurrentDocumentAsync()
    {
        try
        {
            await ViewModel.SaveCurrentDocumentAsync();
            SyncSelection();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("保存文档失败", ex.Message);
        }
    }

    private async void OnDocumentItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not DocumentListItem document)
        {
            return;
        }

        try
        {
            if (document.IsFolder)
            {
                SyncSelection();
                return;
            }

            await ViewModel.SwitchDocumentAsync(document);
            SyncEditorFromViewModel();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("切换文档失败", ex.Message);
        }
    }

    private void OnReaderModeClick(object sender, RoutedEventArgs e)
    {
        var isReaderMode = ReaderModeButton.IsChecked == true;
        ViewModel.SetReaderMode(isReaderMode);
        MarkdownEditorHost.SetReaderMode(isReaderMode);
    }

    private void OnOutlineItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is OutlineListItem outlineItem)
        {
            MarkdownEditorHost.ScrollToHeading(outlineItem.Index);
        }
    }

    private void OnEditorCommandClick(object sender, RoutedEventArgs e)
    {
        if (sender is AppBarButton { Tag: string command })
        {
            MarkdownEditorHost.ApplyMarkdownCommand(command);
        }
    }

    private async void OnReaderAssistantClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string action })
        {
            var result = ViewModel.RunReaderAssistant(action);
            await ShowAssistantResultAsync(result);
        }
    }

    private async void OnEditorMessageReceived(object? sender, EditorMessage message)
    {
        if (message.Type.Equals("document.changed", StringComparison.Ordinal))
        {
            UpdateCurrentMarkdownFromEditor(message);
            return;
        }

        if (message.Type.Equals("document.saveRequested", StringComparison.Ordinal))
        {
            UpdateCurrentMarkdownFromEditor(message);
            await SaveCurrentDocumentAsync();
        }

        if (message.Type.Equals("reader.selectionChanged", StringComparison.Ordinal))
        {
            UpdateReaderSelection(message);
        }
    }

    private void UpdateCurrentMarkdownFromEditor(EditorMessage message)
    {
        if (message.Payload.TryGetProperty("markdown", out var markdownProperty))
        {
            ViewModel.UpdateEditorContent(markdownProperty.GetString() ?? string.Empty);
        }
    }

    private void UpdateReaderSelection(EditorMessage message)
    {
        if (message.Payload.TryGetProperty("selectedText", out var selectedTextProperty))
        {
            ViewModel.UpdateReaderSelection(selectedTextProperty.GetString() ?? string.Empty);
        }
    }

    private async Task<Windows.Storage.StorageFolder?> PickWorkspaceFolderAsync()
    {
        var window = App.CurrentWindow;
        if (window is null)
        {
            throw new InvalidOperationException("主窗口尚未准备好。");
        }

        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List,
            CommitButtonText = "打开工作区"
        };

        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        return await picker.PickSingleFolderAsync();
    }

    private void SyncEditorFromViewModel()
    {
        MarkdownEditorHost.SetMarkdown(ViewModel.CurrentMarkdown);
        SyncSelection();
    }

    private void SyncSelection()
    {
        DocumentTreeList.SelectedItem = ViewModel.Documents.FirstOrDefault(item => item.IsSelected);
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "知道了",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async Task ShowAssistantResultAsync(AssistantPanelItem result)
    {
        var dialog = new ContentDialog
        {
            Title = result.Title,
            Content = result.Summary,
            CloseButtonText = "知道了",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
