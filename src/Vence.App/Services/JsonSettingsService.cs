using System.Text.Json;
using Vence.AI;

namespace Vence.App.Services;

public interface ISettingsService
{
    LlmProviderConfig LoadProviderConfig();

    void SaveProviderConfig(LlmProviderConfig config);

    string SettingsFilePath { get; }
}

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsDirectory;

    public JsonSettingsService()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vence");

        Directory.CreateDirectory(_settingsDirectory);
        SettingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
    }

    public string SettingsFilePath { get; }

    public LlmProviderConfig LoadProviderConfig()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new LlmProviderConfig();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings?.LlmProvider ?? new LlmProviderConfig();
        }
        catch
        {
            return new LlmProviderConfig();
        }
    }

    public void SaveProviderConfig(LlmProviderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        AppSettings settings;

        if (File.Exists(SettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                settings = new AppSettings();
            }
        }
        else
        {
            settings = new AppSettings();
        }

        settings.LlmProvider = config;

        var outputJson = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, outputJson);
    }

    private sealed class AppSettings
    {
        public LlmProviderConfig LlmProvider { get; set; } = new();
    }
}
