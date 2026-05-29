using Microsoft.UI.Xaml.Controls;

namespace Vence.EditorHost;

public sealed class EditorBridge
{
    private WebView2? _webView;

    public event EventHandler<EditorMessage>? MessageReceived;

    public async Task AttachAsync(WebView2 webView, CancellationToken cancellationToken = default)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        await _webView.EnsureCoreWebView2Async();

        cancellationToken.ThrowIfCancellationRequested();

        _webView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            var rawMessage = args.TryGetWebMessageAsString();

            if (EditorMessage.TryParse(rawMessage, out var message) && message is not null)
            {
                MessageReceived?.Invoke(this, message);
            }
        };
    }

    public void Send(EditorMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_webView?.CoreWebView2 is null)
        {
            throw new InvalidOperationException("Editor bridge must be attached before sending messages.");
        }

        _webView.CoreWebView2.PostWebMessageAsString(message.ToJson());
    }
}
