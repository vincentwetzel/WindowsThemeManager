using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Parses Windows .theme files (INI format) into Theme objects.
/// </summary>
public partial class ThemeFileParser : Interfaces.IThemeFileParser
{
    private readonly ILogger<ThemeFileParser> _logger;

    public ThemeFileParser(ILogger<ThemeFileParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Theme> ParseAsync(string themeFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(themeFilePath))
            throw new FileNotFoundException($"Theme file not found: {themeFilePath}", themeFilePath);

        var content = await File.ReadAllTextAsync(themeFilePath, cancellationToken);
        return ParseContent(themeFilePath, content);
    }

    /// <inheritdoc />
    public Theme ParseContent(string themeFilePath, string content)
    {
        var sections = ParseIniSections(content);

        var theme = new Theme
        {
            ThemePath = themeFilePath,
            Name = Path.GetFileNameWithoutExtension(themeFilePath),
            DisplayName = Path.GetFileNameWithoutExtension(themeFilePath), // Default to file name
        };

        // Parse [Theme] section
        if (sections.TryGetValue("Theme", out var themeSection))
        {
            var displayName = GetValue(themeSection, "DisplayName");
            if (!string.IsNullOrEmpty(displayName))
            {
                theme.DisplayName = displayName;
            }
        }

        // Parse [Control Panel\Desktop] section for wallpaper
        if (sections.TryGetValue("Control Panel\\Desktop", out var desktopSection))
        {
            var wallpaper = GetValue(desktopSection, "Wallpaper");
            theme.WallpaperPath = ResolvePath(wallpaper, themeFilePath);
        }

        // Parse [VisualStyles] section
        if (sections.TryGetValue("VisualStyles", out var visualStylesSection))
        {
            var stylePath = GetValue(visualStylesSection, "Path");
            theme.VisualStylePath = ResolvePath(stylePath, themeFilePath);
        }

        // Parse [Cursors] section
        if (sections.TryGetValue("Cursors", out var cursorsSection))
        {
            // The cursors section doesn't have a simple scheme name, but we note its presence
            theme.CursorScheme = cursorsSection.Count > 0 ? "Custom" : null;
        }

        // Parse [Sounds] section
        if (sections.TryGetValue("Sounds", out var soundsSection))
        {
            var schemeName = GetValue(soundsSection, "SchemeName");
            theme.SoundScheme = string.IsNullOrEmpty(schemeName) ? null : schemeName;
        }

        // Determine if it's a system theme based on path
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        theme.IsSystemTheme = themeFilePath.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Parsed theme file: {ThemeFile} -> {DisplayName}",
            themeFilePath, theme.DisplayName);
        Console.WriteLine($"[ThemeFileParser] Parsed theme file: {themeFilePath} -> {theme.DisplayName}");
        System.Diagnostics.Debug.WriteLine($"[ThemeFileParser] Parsed theme file: {themeFilePath} -> {theme.DisplayName}");

        return theme;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIniSections(string content)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();

            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                continue;

            // Section header
            var sectionMatch = SectionRegex().Match(trimmed);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups[1].Value;
                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            // Key=Value pair
            if (currentSection != null)
            {
                var keyValueMatch = KeyValueRegex().Match(trimmed);
                if (keyValueMatch.Success)
                {
                    var key = keyValueMatch.Groups[1].Value;
                    var value = keyValueMatch.Groups[2].Value;
                    sections[currentSection][key] = value;
                }
            }
        }

        return sections;
    }

    private static string? GetValue(Dictionary<string, string> section, string key)
    {
        return section.TryGetValue(key, out var value) ? value : null;
    }

    private static string? ResolvePath(string? path, string themeFilePath)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Remove quotes if present
        path = path.Trim('"');

        // If already absolute, return it (even if file doesn't exist yet)
        if (Path.IsPathRooted(path))
            return path;

        // Try to resolve relative to theme file location
        var themeDir = Path.GetDirectoryName(themeFilePath);
        if (!string.IsNullOrEmpty(themeDir))
        {
            var relativePath = Path.Combine(themeDir, path);
            // Return resolved path regardless of existence - theme files often reference future/missing files
            return relativePath;
        }

        // Return as-is if we can't resolve
        return path;
    }

    [GeneratedRegex(@"^\[([^\]]+)\]$", RegexOptions.Compiled)]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"^([^=]+)=(.*)$", RegexOptions.Compiled)]
    private static partial Regex KeyValueRegex();
}
