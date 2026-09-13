namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Scans Windows theme directories to discover available themes.
/// </summary>
public interface IThemeDirectoryScanner
{
    /// <summary>
    /// Scans all known Windows theme directories and returns paths to .theme files.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of theme file paths found on the system.</returns>
    IEnumerable<string> ScanThemeDirectories(CancellationToken cancellationToken = default);

}
