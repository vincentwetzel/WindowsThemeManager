using Microsoft.Extensions.Logging.Abstractions;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;

namespace WindowsThemeManager.Tests.Unit;

public class ThemeFileParserTests
{
    private readonly ThemeFileParser _parser;

    public ThemeFileParserTests()
    {
        var logger = NullLogger<ThemeFileParser>.Instance;
        _parser = new ThemeFileParser(logger);
    }

    [Fact]
    public void ParseContent_ValidTheme_ReturnsThemeWithCorrectName()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=My Custom Theme

            [Control Panel\Desktop]
            Wallpaper=C:\Wallpapers\test.jpg
            """;

        // Act
        var theme = _parser.ParseContent("C:\\test.theme", content);

        // Assert
        Assert.Equal("My Custom Theme", theme.DisplayName);
        Assert.Equal("test", theme.Name);
    }

    [Fact]
    public void ParseContent_WithWallpaper_SetsWallpaperPath()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=Wallpaper Theme

            [Control Panel\Desktop]
            Wallpaper=%SystemRoot%\Web\Wallpaper\test.jpg
            """;

        // Act
        var theme = _parser.ParseContent("C:\\test.theme", content);

        // Assert
        Assert.NotNull(theme.WallpaperPath);
    }

    [Fact]
    public void ParseContent_WithVisualStyles_SetsVisualStylePath()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=Styled Theme

            [VisualStyles]
            Path=C:\Windows\resources\Themes\aero.msstyles
            """;

        // Act
        var theme = _parser.ParseContent("C:\\test.theme", content);

        // Assert
        Assert.NotNull(theme.VisualStylePath);
    }

    [Fact]
    public void ParseContent_WithSounds_SetsSoundScheme()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=Complete Theme

            [Sounds]
            SchemeName=Windows Default
            """;

        // Act
        var theme = _parser.ParseContent("C:\\test.theme", content);

        // Assert
        Assert.Equal("Windows Default", theme.SoundScheme);
    }

    [Fact]
    public void ParseContent_EmptyTheme_ReturnsThemeWithDefaults()
    {
        // Arrange
        const string content = "";

        // Act
        var theme = _parser.ParseContent("C:\\minimal.theme", content);

        // Assert
        Assert.Equal("minimal", theme.DisplayName);
        Assert.Null(theme.WallpaperPath);
        Assert.Null(theme.VisualStylePath);
    }

    [Fact]
    public void ParseContent_SystemTheme_MarkedAsSystemTheme()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=Windows Default
            """;
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var themePath = Path.Combine(windowsDir, "Resources", "Themes", "aero.theme");

        // Act
        var theme = _parser.ParseContent(themePath, content);

        // Assert
        Assert.True(theme.IsSystemTheme);
    }

    [Fact]
    public void ParseContent_UserTheme_NotMarkedAsSystemTheme()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=My Theme
            """;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var themePath = Path.Combine(appData, "Microsoft", "Windows", "Themes", "mytheme.theme");

        // Act
        var theme = _parser.ParseContent(themePath, content);

        // Assert
        Assert.False(theme.IsSystemTheme);
    }

    [Fact]
    public void ParseContent_FullTheme_AllPropertiesSet()
    {
        // Arrange
        const string content = """
            [Theme]
            DisplayName=Full Featured Theme

            [Control Panel\Desktop]
            Wallpaper=C:\Images\wallpaper.jpg

            [VisualStyles]
            Path=C:\Windows\resources\Themes\custom.msstyles

            [Cursors]
            Arrow=arrow.cur

            [Sounds]
            SchemeName=No Sounds
            """;

        // Act
        var theme = _parser.ParseContent("C:\\full.theme", content);

        // Assert
        Assert.Equal("Full Featured Theme", theme.DisplayName);
        Assert.NotNull(theme.WallpaperPath);
        Assert.NotNull(theme.VisualStylePath);
        Assert.NotNull(theme.CursorScheme);
        Assert.Equal("No Sounds", theme.SoundScheme);
    }

    [Fact]
    public async Task ParseAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _parser.ParseAsync("C:\\nonexistent.theme"));
    }
}
