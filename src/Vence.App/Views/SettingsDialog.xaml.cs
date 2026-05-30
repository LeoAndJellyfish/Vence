using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Vence.AI;

namespace Vence.App.Views;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly LlmProviderConfig _originalConfig;

    public LlmProviderConfig ResultConfig { get; private set; }

    public SettingsDialog(LlmProviderConfig currentConfig)
    {
        _originalConfig = currentConfig ?? new LlmProviderConfig();
        ResultConfig = new LlmProviderConfig();

        InitializeComponent();

        ProviderComboBox.SelectedIndex = (int)_originalConfig.ProviderType;
        ApiKeyBox.Password = _originalConfig.ApiKey;
        EndpointBox.Text = _originalConfig.Endpoint;
        EndpointBox.PlaceholderText = LlmProviderConfig.GetDefaultEndpoint(_originalConfig.ProviderType);
        ModelIdBox.PlaceholderText = LlmProviderConfig.GetDefaultModelId(_originalConfig.ProviderType);
        ModelIdBox.Text = _originalConfig.ModelId;

        UpdateProviderUI(_originalConfig.ProviderType);
        UpdateConfigStatus();

        ProviderComboBox.SelectionChanged += OnProviderChanged;
        ApiKeyBox.PasswordChanged += OnFieldChanged;
        EndpointBox.TextChanged += OnFieldChanged;
        ModelIdBox.TextChanged += OnFieldChanged;
    }

    private void OnProviderChanged(object sender, SelectionChangedEventArgs e)
    {
        var providerType = (LlmProviderType)ProviderComboBox.SelectedIndex;
        ModelIdBox.PlaceholderText = LlmProviderConfig.GetDefaultModelId(providerType);
        EndpointBox.PlaceholderText = LlmProviderConfig.GetDefaultEndpoint(providerType);
        UpdateProviderUI(providerType);
        UpdateConfigStatus();
    }

    private void OnProviderComboBoxPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnFieldChanged(object sender, object e)
    {
        UpdateConfigStatus();
    }

    private void UpdateProviderUI(LlmProviderType providerType)
    {
        ApiKeySection.Visibility = providerType == LlmProviderType.OpenAI
            ? Visibility.Visible
            : Visibility.Collapsed;

        ProviderSummaryText.Text = providerType switch
        {
            LlmProviderType.OpenAI => "连接云端或兼容 OpenAI 的代理接口，用于语法检查、润色、补全和结构建议。",
            LlmProviderType.Ollama => "连接本机 Ollama 服务，适合离线写作场景。请确认服务已启动并已下载模型。",
            _ => string.Empty
        };

        EndpointHintText.Text = providerType switch
        {
            LlmProviderType.OpenAI => "默认端点为 https://api.openai.com/v1；使用第三方兼容接口时在这里覆盖。",
            LlmProviderType.Ollama => "默认端点为 http://localhost:11434；如果 Ollama 运行在其他地址，请填写完整 URL。",
            _ => string.Empty
        };

        ModelHintText.Text = providerType switch
        {
            LlmProviderType.OpenAI => "示例：gpt-4o-mini。请填写账号可用的模型名称。",
            LlmProviderType.Ollama => "示例：llama3.2。名称需要与本机 Ollama 模型列表一致。",
            _ => string.Empty
        };
    }

    private void UpdateConfigStatus()
    {
        var config = BuildConfig();
        if (config.IsConfigured)
        {
            ConfigStatusInfoBar.Severity = InfoBarSeverity.Success;
            ConfigStatusInfoBar.Title = "已配置";
            ConfigStatusInfoBar.Message = "AI 配置完整，可以正常使用。";
        }
        else
        {
            ConfigStatusInfoBar.Severity = InfoBarSeverity.Warning;
            ConfigStatusInfoBar.Title = "未配置";
            ConfigStatusInfoBar.Message = "请填写 AI 配置后保存，读者模式 AI 功能才能使用。";
        }
    }

    private LlmProviderConfig BuildConfig()
    {
        return new LlmProviderConfig
        {
            ProviderType = (LlmProviderType)ProviderComboBox.SelectedIndex,
            ApiKey = ApiKeyBox.Password,
            Endpoint = EndpointBox.Text.Trim(),
            ModelId = ModelIdBox.Text.Trim()
        };
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ResultConfig = BuildConfig();
    }
}
