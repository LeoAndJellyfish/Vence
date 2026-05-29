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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        await _bridge.AttachAsync(EditorWebView);
        _bridge.MessageReceived += (_, message) => EditorMessageReceived?.Invoke(this, message);

        EditorWebView.NavigateToString(EditorHostHtml.Create(InitialMarkdown));
    }
}
