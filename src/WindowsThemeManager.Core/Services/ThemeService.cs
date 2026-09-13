using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Orchestrates theme discovery, parsing, and application.
/// Combines the directory scanner and file parser to provide a complete theme service.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IThemeDirectoryScanner _scanner;
    private readonly IThemeFileParser _parser;
    private readonly IThemeApplier _applier;
    private readonly ILogger<ThemeService> _logger;

    private readonly object _cacheLock = new();
    private List<Theme>? _cachedThemes;
    private DateTime _cacheTimestamp;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public event EventHandler<Theme>? ThemeChanged;

    public ThemeService(
        IThemeDirectoryScanner scanner,
        IThemeFileParser parser,
        IThemeApplier applier,
        ILogger<ThemeService> logger)
    {
        _scanner = scanner;
        _parser = parser;
        _applier = applier;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Theme>> DiscoverThemesAsync(CancellationToken cancellationToken = default)
    {
        // Check cache
        lock (_cacheLock)
        {
            if (_cachedThemes != null && DateTime.UtcNow - _cacheTimestamp < CacheDuration)
            {
                _logger.LogDebug("Returning cached themes ({Count})", _cachedThemes.Count);
                return _cachedThemes;
            }
        }

        _logger.LogInformation("Discovering themes...");

        var themePaths = _scanner.ScanThemeDirectories(cancellationToken);
        var themes = new List<Theme>();

        foreach (var path in themePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var theme = await _parser.ParseAsync(path, cancellationToken);
                themes.Add(theme);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to parse theme file: {ThemeFile}", path);
            }
        }

        // Sort by display name
        themes.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

        // Update cache
        lock (_cacheLock)
        {
            _cachedThemes = themes;
            _cacheTimestamp = DateTime.UtcNow;
        }

        _logger.LogInformation("Discovered {Count} themes", themes.Count);
        return themes;
    }

    /// <inheritdoc />
    public async Task<Theme?> GetCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        var themes = await DiscoverThemesAsync(cancellationToken);

        // Read current wallpaper from registry to try to match a theme
        var currentWallpaper = GetCurrentWallpaperPath();

        if (!string.IsNullOrEmpty(currentWallpaper))
        {
            // Try to find a theme that matches the current wallpaper
            var matchingTheme = themes.FirstOrDefault(t =>
                string.Equals(t.WallpaperPath, currentWallpaper, StringComparison.OrdinalIgnoreCase));

            if (matchingTheme != null)
            {
                _logger.LogDebug("Found matching theme: {DisplayName}", matchingTheme.DisplayName);
                return matchingTheme;
            }
        }

        _logger.LogDebug("Could not determine current theme");
        return null;
    }

    /// <inheritdoc />
    public async Task ApplyThemeAsync(Theme theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (!theme.IsValid)
            throw new InvalidOperationException($"Cannot apply invalid theme: {theme.DisplayName}");

        _logger.LogInformation("Applying theme: {DisplayName}", theme.DisplayName);

        var success = await _applier.ApplyThemeAsync(theme, cancellationToken);

        if (success)
        {
            InvalidateCache();

            ThemeChanged?.Invoke(this, theme);
        }
        else
        {
            _logger.LogWarning("Theme application failed: {DisplayName}", theme.DisplayName);
            throw new InvalidOperationException($"Failed to apply theme: {theme.DisplayName}");
        }
    }

    /// <inheritdoc />
    public void RefreshThemes()
    {
        InvalidateCache();

        _logger.LogInformation("Theme cache cleared");
    }

    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedThemes = null;
            _cacheTimestamp = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Reads the current wallpaper path from the registry.
    /// </summary>
    private static string? GetCurrentWallpaperPath()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            return key?.GetValue("WallPaper") as string;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
