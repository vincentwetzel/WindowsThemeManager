namespace WindowsThemeManager.Core.Models;

/// <summary>
/// Represents a discovered Windows theme.
/// </summary>
public class Theme
{
    /// <summary>
    /// The display name shown to users.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the .theme file.
    /// </summary>
    public string ThemePath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the wallpaper image file.
    /// </summary>
    public string? WallpaperPath { get; set; }

    /// <summary>
    /// Indicates whether this is a system-provided theme.
    /// </summary>
    public bool IsSystemTheme { get; set; }

    /// <summary>
    /// Returns true if the theme file exists and is valid.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(ThemePath) && File.Exists(ThemePath);

    public override string ToString() => DisplayName;
}
