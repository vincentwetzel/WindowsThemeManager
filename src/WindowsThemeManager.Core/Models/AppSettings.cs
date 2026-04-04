namespace WindowsThemeManager.Core.Models;

/// <summary>
/// User settings persisted between sessions.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Width of the left theme panel.
    /// </summary>
    public double ThemePanelWidth { get; set; } = 300;

    /// <summary>
    /// Main window width.
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Main window height.
    /// </summary>
    public double WindowHeight { get; set; } = 700;

    /// <summary>
    /// Whether the window was maximized on last close.
    /// </summary>
    public bool WindowMaximized { get; set; }

    /// <summary>
    /// Additional directories to scan for themes (user-defined).
    /// </summary>
    public List<string> CustomThemeDirectories { get; set; } = new();

    /// <summary>
    /// Maximum number of cached theme previews (0 = unlimited).
    /// </summary>
    public int MaxCacheSize { get; set; } = 100;

    /// <summary>
    /// Whether to refresh on startup.
    /// </summary>
    public bool RefreshOnStartup { get; set; } = true;

    /// <summary>
    /// Thumbnail size for monitor previews.
    /// </summary>
    public int ThumbnailMaxWidth { get; set; } = 400;

    /// <summary>
    /// Thumbnail height for monitor previews.
    /// </summary>
    public int ThumbnailMaxHeight { get; set; } = 300;

    /// <summary>
    /// Application theme mode (Light, Dark, or System).
    /// </summary>
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    /// <summary>
    /// Gets the default settings.
    /// </summary>
    public static AppSettings Default => new();
}
