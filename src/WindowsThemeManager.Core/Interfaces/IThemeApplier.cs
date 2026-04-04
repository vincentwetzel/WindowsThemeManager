using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Service responsible for applying themes (wallpaper, visual styles, etc.).
/// </summary>
public interface IThemeApplier
{
    /// <summary>
    /// Applies a complete theme including wallpaper and visual styles.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the theme was applied successfully.</returns>
    Task<bool> ApplyThemeAsync(Theme theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets only the wallpaper without changing other theme components.
    /// </summary>
    /// <param name="wallpaperPath">Path to the wallpaper image.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetWallpaperAsync(string? wallpaperPath, CancellationToken cancellationToken = default);
}
