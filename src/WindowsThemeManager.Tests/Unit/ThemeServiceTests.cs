using Microsoft.Extensions.Logging.Abstractions;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;

namespace WindowsThemeManager.Tests.Unit;

public class ThemeServiceTests
{
    private readonly ThemeService _themeService;
    private readonly ThemeDirectoryScanner _scanner;
    private readonly ThemeFileParser _parser;
    private readonly ThemeApplier _applier;

    public ThemeServiceTests()
    {
        var scannerLogger = NullLogger<ThemeDirectoryScanner>.Instance;
        var parserLogger = NullLogger<ThemeFileParser>.Instance;
        var serviceLogger = NullLogger<ThemeService>.Instance;
        var applierLogger = NullLogger<ThemeApplier>.Instance;

        _scanner = new ThemeDirectoryScanner(scannerLogger);
        _parser = new ThemeFileParser(parserLogger);
        _applier = new ThemeApplier(applierLogger);
        _themeService = new ThemeService(_scanner, _parser, _applier, serviceLogger);
    }

    [Fact]
    public async Task DiscoverThemesAsync_ReturnsValidThemes()
    {
        // Act
        var themes = await _themeService.DiscoverThemesAsync();

        // Assert
        Assert.NotNull(themes);

        foreach (var theme in themes)
        {
            Assert.NotNull(theme.DisplayName);
            Assert.NotNull(theme.ThemePath);
            Assert.True(File.Exists(theme.ThemePath), $"Theme file should exist: {theme.ThemePath}");
        }
    }

    [Fact]
    public async Task GetCurrentThemeAsync_ReturnsValidThemeOrNull()
    {
        // Act
        var currentTheme = await _themeService.GetCurrentThemeAsync();

        // Assert - may be null if no matching theme found
        if (currentTheme != null)
        {
            Assert.NotNull(currentTheme.DisplayName);
            Assert.NotNull(currentTheme.ThemePath);
        }
    }

    [Fact]
    public async Task RefreshThemesAsync_ClearsCache()
    {
        // Act
        await _themeService.DiscoverThemesAsync(); // populate cache
        await _themeService.RefreshThemesAsync();
        var themes = await _themeService.DiscoverThemesAsync(); // should re-scan

        // Assert - should not throw
        Assert.NotNull(themes);
    }

    [Fact]
    public async Task ApplyThemeAsync_NullTheme_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _themeService.ApplyThemeAsync(null!));
    }

    [Fact]
    public async Task ApplyThemeAsync_InvalidTheme_ThrowsInvalidOperationException()
    {
        // Arrange - create a theme with a non-existent file
        var invalidTheme = new Theme
        {
            ThemePath = "C:\\nonexistent\\theme.theme",
            DisplayName = "Invalid Theme"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _themeService.ApplyThemeAsync(invalidTheme));
    }
}
