using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Parses Windows .theme files (INI format) into Theme objects.
/// </summary>
public interface IThemeFileParser
{
    /// <summary>
    /// Parses a .theme file and returns a Theme object.
    /// </summary>
    /// <param name="themeFilePath">Full path to the .theme file.</param>
    /// <returns>The parsed Theme object.</returns>
    Task<Theme> ParseAsync(string themeFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses theme file content directly.
    /// </summary>
    /// <param name="themeFilePath">Path for context (used for relative path resolution).</param>
    /// <param name="content">Raw content of the .theme file.</param>
    /// <returns>The parsed Theme object.</returns>
    Theme ParseContent(string themeFilePath, string content);
}
