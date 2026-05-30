using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Vence.EditorHost;

public sealed partial class EditorHostView : UserControl
{
    public static readonly DependencyProperty InitialMarkdownProperty =
        DependencyProperty.Register(
            nameof(InitialMarkdown),
            typeof(string),
            typeof(EditorHostView),
            new PropertyMetadata(string.Empty));

    private readonly EditorBridge _bridge = new();
    private bool _isReady;

    public EditorHostView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public string InitialMarkdown
    {
        get => (string)GetValue(InitialMarkdownProperty);
        set => SetValue(InitialMarkdownProperty, value);
    }

    public event EventHandler<EditorMessage>? EditorMessageReceived;

    public void SetMarkdown(string markdown)
    {
        InitialMarkdown = markdown;

        if (!_isReady)
        {
            return;
        }

        _bridge.Send(EditorMessage.Create("document.setMarkdown", new { markdown }));
    }

    public void SetReaderMode(bool isReaderMode)
    {
        if (!_isReady)
        {
            return;
        }

        _bridge.Send(EditorMessage.Create("document.setReaderMode", new { enabled = isReaderMode }));
    }

    public void ApplyMarkdownCommand(string command)
    {
        if (!_isReady)
        {
            return;
        }

        _bridge.Send(EditorMessage.Create("editor.applyMarkdownCommand", new { command }));
    }

    public void ScrollToHeading(int headingIndex)
    {
        if (!_isReady)
        {
            return;
        }

        _bridge.Send(EditorMessage.Create("document.scrollToHeading", new { headingIndex }));
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        await _bridge.AttachAsync(EditorWebView);
        _bridge.MessageReceived += (_, message) => EditorMessageReceived?.Invoke(this, message);
        EditorWebView.NavigationCompleted += (_, _) =>
        {
            _isReady = true;
            SetMarkdown(InitialMarkdown);
        };

        EditorWebView.NavigateToString(EditorHostHtml.Create(InitialMarkdown));
    }
}
