namespace WindowsThemeManager.Core.Models;

/// <summary>
/// Represents information about a single monitor in the system.
/// </summary>
public class MonitorInfo
{
    /// <summary>
    /// The device name (e.g., "\\.\DISPLAY1").
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// The monitor number (1-based).
    /// </summary>
    public int MonitorNumber { get; set; }

    /// <summary>
    /// The bounds of the monitor in virtual screen coordinates.
    /// </summary>
    public IntRect Bounds { get; set; }

    /// <summary>
    /// The working area of the monitor (excluding taskbar).
    /// </summary>
    public IntRect WorkingArea { get; set; }

    /// <summary>
    /// Indicates whether this is the primary monitor.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Path to the current wallpaper displayed on this monitor.
    /// </summary>
    public string? CurrentWallpaperPath { get; set; }

    /// <summary>
    /// Thumbnail preview of the current wallpaper.
    /// </summary>
    public object? WallpaperPreview { get; set; }

    public override string ToString() => $"{DeviceName} ({Bounds.Width}x{Bounds.Height}){(IsPrimary ? " - Primary" : "")}";
}
