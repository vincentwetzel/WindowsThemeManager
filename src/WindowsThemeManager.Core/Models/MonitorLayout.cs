namespace WindowsThemeManager.Core.Models;

/// <summary>
/// Represents the complete monitor layout configuration.
/// </summary>
public class MonitorLayout
{
    /// <summary>
    /// List of all connected monitors information.
    /// </summary>
    public List<MonitorInfo> Monitors { get; set; } = new();

    /// <summary>
    /// The total bounds encompassing all monitors.
    /// </summary>
    public IntRect TotalBounds { get; set; }

    /// <summary>
    /// Returns true if there is more than one monitor.
    /// </summary>
    public bool IsMultiMonitor => Monitors.Count > 1;

    /// <summary>
    /// Gets the primary monitor.
    /// </summary>
    public MonitorInfo? PrimaryMonitor => Monitors.FirstOrDefault(m => m.IsPrimary);
}
