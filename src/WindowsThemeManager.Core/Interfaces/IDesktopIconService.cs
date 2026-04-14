using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Service for saving and restoring desktop icon positions.
/// </summary>
public interface IDesktopIconService
{
    /// <summary>
    /// Captures the current desktop icon layout.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current desktop icon layout.</returns>
    Task<DesktopIconLayout> CaptureLayoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a previously saved desktop icon layout.
    /// </summary>
    /// <param name="layout">The layout to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restoration was successful.</returns>
    Task<bool> RestoreLayoutAsync(DesktopIconLayout layout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a layout to a JSON file.
    /// </summary>
    /// <param name="layout">The layout to save.</param>
    /// <param name="filePath">Optional file path. If null, uses default location.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file path where the layout was saved.</returns>
    Task<string> SaveLayoutToFileAsync(DesktopIconLayout layout, string? filePath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a layout from a JSON file.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded desktop icon layout.</returns>
    Task<DesktopIconLayout> LoadLayoutFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default directory for storing layout files.
    /// </summary>
    string LayoutsDirectory { get; }
}
