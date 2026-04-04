namespace WindowsThemeManager.Core.Models;

/// <summary>
/// Represents a Windows theme with all its associated components.
/// </summary>
public class Theme
{
    /// <summary>
    /// The internal name of the theme.
    /// </summary>
    public string Name { get; set; } = string.Empty;

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
    /// Path to the visual style (.msstyles) file.
    /// </summary>
    public string? VisualStylePath { get; set; }

    /// <summary>
    /// The cursor scheme name or path.
    /// </summary>
    public string? CursorScheme { get; set; }

    /// <summary>
    /// The sound scheme name.
    /// </summary>
    public string? SoundScheme { get; set; }

    /// <summary>
    /// Path to the theme preview image.
    /// </summary>
    public string? PreviewImage { get; set; }

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
