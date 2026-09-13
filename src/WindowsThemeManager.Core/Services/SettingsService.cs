using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Persists and loads user settings to a JSON file.
/// </summary>
public class SettingsService
{
    private static readonly JsonSerializerOptions LoadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonSerializerOptions SaveOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;

    public AppSettings Settings { get; private set; } = AppSettings.Default;

    public SettingsService(ILogger<SettingsService> logger, string? customFilePath = null)
    {
        _logger = logger;
        _settingsFilePath = customFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsThemeManager",
            "settings.json");
    }

    /// <summary>
    /// Loads settings from disk, or creates defaults if not found.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogDebug("Settings file not found, using defaults: {Path}", _settingsFilePath);
                Settings = AppSettings.Default;
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, LoadOptions) ?? AppSettings.Default;
            _logger.LogInformation("Settings loaded from {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            Settings = AppSettings.Default;
        }
    }

    /// <summary>
    /// Saves current settings to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Settings, SaveOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            _logger.LogInformation("Settings saved to {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
        }
    }
}
