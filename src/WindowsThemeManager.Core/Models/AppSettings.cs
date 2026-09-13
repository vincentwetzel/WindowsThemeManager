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
    /// Application theme mode (Light, Dark, or System).
    /// </summary>
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    /// <summary>
    /// Gets the default settings.
    /// </summary>
    public static AppSettings Default => new();
}
