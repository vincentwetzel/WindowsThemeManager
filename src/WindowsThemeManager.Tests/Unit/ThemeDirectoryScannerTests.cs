using Microsoft.Extensions.Logging.Abstractions;
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
    public void ScanThemeDirectories_ReturnsExistingPaths()
    {
        // Act
        var themePaths = _scanner.ScanThemeDirectories();

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
