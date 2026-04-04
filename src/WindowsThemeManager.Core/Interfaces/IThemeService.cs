using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Service for discovering and managing Windows themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Discovers all available themes on the system.
    /// </summary>
    Task<IEnumerable<Theme>> DiscoverThemesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    Task<Theme?> GetCurrentThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the specified theme.
    /// </summary>
    Task ApplyThemeAsync(Theme theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a theme is successfully applied.
    /// </summary>
    event EventHandler<Theme>? ThemeChanged;

    /// <summary>
    /// Refreshes the theme cache and re-scans directories.
    /// </summary>
    Task RefreshThemesAsync(CancellationToken cancellationToken = default);
}
