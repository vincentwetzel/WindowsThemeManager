using Microsoft.Extensions.Logging.Abstractions;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;

namespace WindowsThemeManager.Tests.Unit;

public class SettingsServiceTests
{
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        var logger = NullLogger<SettingsService>.Instance;
        _settingsService = new SettingsService(logger);
    }

    [Fact]
    public void Constructor_DefaultSettings_AreValid()
    {
        // Assert - settings should default to valid values
        Assert.Equal(300, _settingsService.Settings.ThemePanelWidth);
        Assert.Equal(1200, _settingsService.Settings.WindowWidth);
        Assert.Equal(700, _settingsService.Settings.WindowHeight);
        Assert.False(_settingsService.Settings.WindowMaximized);
        Assert.Empty(_settingsService.Settings.CustomThemeDirectories);
        Assert.Equal(100, _settingsService.Settings.MaxCacheSize);
        Assert.True(_settingsService.Settings.RefreshOnStartup);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesSettings()
    {
        // Arrange - use a unique temp file to avoid polluting other tests
        var tempPath = Path.Combine(Path.GetTempPath(), $"wtm_test_settings_{Guid.NewGuid():N}.json");
        var logger = NullLogger<SettingsService>.Instance;
        var service = new SettingsService(logger, tempPath);

        service.Settings.ThemePanelWidth = 400;
        service.Settings.WindowWidth = 1400;
        service.Settings.WindowHeight = 900;
        service.Settings.WindowMaximized = true;
        service.Settings.RefreshOnStartup = false;

        // Act
        await service.SaveAsync();

        // Create a new instance to verify loading from disk
        var newService = new SettingsService(logger, tempPath);
        await newService.LoadAsync();

        // Assert
        Assert.Equal(400, newService.Settings.ThemePanelWidth);
        Assert.Equal(1400, newService.Settings.WindowWidth);
        Assert.Equal(900, newService.Settings.WindowHeight);
        Assert.True(newService.Settings.WindowMaximized);
        Assert.False(newService.Settings.RefreshOnStartup);

        // Cleanup
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }

    [Fact]
    public async Task Load_NonExistentFile_UsesDefaults()
    {
        // Arrange - use a unique temp path that doesn't exist
        var tempPath = Path.Combine(Path.GetTempPath(), $"wtm_test_defaults_{Guid.NewGuid():N}.json");
        var logger = NullLogger<SettingsService>.Instance;
        var service = new SettingsService(logger, tempPath);

        // Act
        await service.LoadAsync();

        // Assert
        Assert.Equal(300, service.Settings.ThemePanelWidth);
        Assert.Equal(1200, service.Settings.WindowWidth);

        // Cleanup (shouldn't exist, but be safe)
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }
}
