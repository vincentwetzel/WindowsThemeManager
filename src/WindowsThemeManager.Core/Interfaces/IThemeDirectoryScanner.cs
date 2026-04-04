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
    Task<IEnumerable<string>> ScanThemeDirectoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of directories that will be scanned for themes.
    /// </summary>
    IEnumerable<string> GetThemeDirectories();
}
