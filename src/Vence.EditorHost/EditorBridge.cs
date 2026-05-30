using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Vence.EditorHost;

public sealed class EditorBridge
{
    private const string VirtualHostName = "vence.local";
    private const string ScriptsFolderName = "Scripts";

    private WebView2? _webView;

    public event EventHandler<EditorMessage>? MessageReceived;

    public static string ScriptsOrigin => $"https://{VirtualHostName}";

    public async Task AttachAsync(WebView2 webView, CancellationToken cancellationToken = default)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        await _webView.EnsureCoreWebView2Async();

        cancellationToken.ThrowIfCancellationRequested();

        var scriptsPath = Path.Combine(AppContext.BaseDirectory, ScriptsFolderName);
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            VirtualHostName,
            scriptsPath,
            CoreWebView2HostResourceAccessKind.Allow);

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
