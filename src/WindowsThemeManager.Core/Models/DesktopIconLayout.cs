namespace WindowsThemeManager.Core.Models;

/// <summary>
/// Represents the position of a single desktop icon.
/// </summary>
public record DesktopIconPosition
{
    /// <summary>
    /// The name/label of the icon (e.g., "Recycle Bin", "This PC").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// X coordinate in physical pixels.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Y coordinate in physical pixels.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Whether the icon is visible.
    /// </summary>
    public bool IsVisible { get; init; } = true;
}

/// <summary>
/// Represents a complete desktop icon layout snapshot.
/// </summary>
public record DesktopIconLayout
{
    /// <summary>
    /// Timestamp when this layout was saved.
    /// </summary>
    public DateTime SavedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Display resolution when this layout was captured (e.g., "1920x1080").
    /// </summary>
    public string DisplayResolution { get; init; } = string.Empty;

    /// <summary>
    /// DPI scaling percentage when captured (e.g., 100, 125, 150).
    /// </summary>
    public int DpiScale { get; init; }

    /// <summary>
    /// List of all desktop icons and their positions.
    /// </summary>
    public List<DesktopIconPosition> Icons { get; init; } = new();

    /// <summary>
    /// Optional name/label for this layout configuration.
    /// </summary>
    public string LayoutName { get; init; } = string.Empty;
}
