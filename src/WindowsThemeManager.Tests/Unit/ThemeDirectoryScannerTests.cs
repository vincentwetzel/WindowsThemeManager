using Microsoft.Extensions.Logging.Abstractions;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;

namespace WindowsThemeManager.Tests.Unit;

public class ThemeDirectoryScannerTests
{
    private readonly ThemeDirectoryScanner _scanner;

    public ThemeDirectoryScannerTests()
    {
        var logger = NullLogger<ThemeDirectoryScanner>.Instance;
        _scanner = new ThemeDirectoryScanner(logger);
    }

    [Fact]
    public void GetThemeDirectories_ReturnsNonEmptyList()
    {
        // Act
        var directories = _scanner.GetThemeDirectories();

        // Assert - should include standard Windows theme directories
        Assert.NotEmpty(directories);
        Assert.Contains(directories, d => d.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanThemeDirectoriesAsync_ReturnsExistingPaths()
    {
        // Act
        var themePaths = await _scanner.ScanThemeDirectoriesAsync();

        // Assert - may be empty if no themes installed, but should not throw
        Assert.NotNull(themePaths);

        // Any paths returned should exist
        foreach (var path in themePaths)
        {
            Assert.True(File.Exists(path), $"Theme file should exist: {path}");
            Assert.EndsWith(".theme", path, StringComparison.OrdinalIgnoreCase);
        }
    }
}
