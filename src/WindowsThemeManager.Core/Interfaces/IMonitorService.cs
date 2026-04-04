using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Service for detecting and managing monitor configuration.
/// </summary>
public interface IMonitorService
{
    /// <summary>
    /// Gets the current monitor layout configuration.
    /// </summary>
    Task<MonitorLayout> GetMonitorLayoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the wallpaper path for a specific monitor.
    /// </summary>
    Task<string?> GetMonitorWallpaperAsync(int monitorIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts listening for wallpaper change events.
    /// </summary>
    void StartListeningForWallpaperChanges();

    /// <summary>
    /// Stops listening for wallpaper change events.
    /// </summary>
    void StopListeningForWallpaperChanges();

    /// <summary>
    /// Event raised when a monitor's wallpaper changes.
    /// Provides the monitor device path and new wallpaper path.
    /// </summary>
    event EventHandler<(string DevicePath, string? WallpaperPath)>? WallpaperChanged;

    /// <summary>
    /// Event raised when monitor configuration changes.
    /// </summary>
    event EventHandler? MonitorConfigurationChanged;
}
