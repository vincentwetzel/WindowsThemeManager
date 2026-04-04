using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Persists and loads user settings to a JSON file.
/// </summary>
public class SettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;

    public AppSettings Settings { get; private set; } = AppSettings.Default;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsThemeManager",
            "settings.json");
    }

    /// <summary>
    /// Creates a SettingsService with a custom file path (for testing).
    /// </summary>
    public SettingsService(ILogger<SettingsService> logger, string customFilePath)
    {
        _logger = logger;
        _settingsFilePath = customFilePath;
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
                Console.WriteLine($"[SettingsService] Settings file not found, using defaults: {_settingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Settings file not found, using defaults: {_settingsFilePath}");
                Settings = AppSettings.Default;
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            Settings = JsonSerializer.Deserialize<AppSettings>(json, options) ?? AppSettings.Default;
            _logger.LogInformation("Settings loaded from {Path}", _settingsFilePath);
            Console.WriteLine($"[SettingsService] Settings loaded from {_settingsFilePath}");
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Settings loaded from {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            Console.WriteLine($"[SettingsService] Failed to load settings, using defaults: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to load settings, using defaults: {ex.Message}");
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(Settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            _logger.LogInformation("Settings saved to {Path}", _settingsFilePath);
            Console.WriteLine($"[SettingsService] Settings saved to {_settingsFilePath}");
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Settings saved to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
            Console.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
        }
    }
}
