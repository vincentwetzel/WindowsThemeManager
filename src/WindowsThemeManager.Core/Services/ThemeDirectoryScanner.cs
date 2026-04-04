using Microsoft.Extensions.Logging;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Scans standard Windows theme directories to discover .theme files.
/// </summary>
public class ThemeDirectoryScanner : Interfaces.IThemeDirectoryScanner
{
    private static readonly string[] ThemeDirectoryPaths =
    {
        // Current user themes
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Themes"),

        // System themes
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Resources", "Themes"),

        // Roaming user themes
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Themes"),

        // Additional common locations
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft", "Windows", "Themes"),
    };

    private readonly ILogger<ThemeDirectoryScanner> _logger;

    public ThemeDirectoryScanner(ILogger<ThemeDirectoryScanner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetThemeDirectories()
    {
        return ThemeDirectoryPaths;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ScanThemeDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var themePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in ThemeDirectoryPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(directory))
            {
                _logger.LogDebug("Theme directory not found, skipping: {Directory}", directory);
                Console.WriteLine($"[ThemeDirectoryScanner] Theme directory not found, skipping: {directory}");
                System.Diagnostics.Debug.WriteLine($"[ThemeDirectoryScanner] Theme directory not found, skipping: {directory}");
                continue;
            }

            try
            {
                // Scan the directory itself
                await ScanDirectoryAsync(directory, themePaths, cancellationToken);

                // Also scan subdirectories (themes often have their own folders)
                foreach (var subDirectory in Directory.EnumerateDirectories(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ScanDirectoryAsync(subDirectory, themePaths, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied to theme directory: {Directory}", directory);
                Console.WriteLine($"[ThemeDirectoryScanner] Access denied to theme directory: {directory} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ThemeDirectoryScanner] Access denied to theme directory: {directory} - {ex.Message}");
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "IO error scanning theme directory: {Directory}", directory);
                Console.WriteLine($"[ThemeDirectoryScanner] IO error scanning theme directory: {directory} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ThemeDirectoryScanner] IO error scanning theme directory: {directory} - {ex.Message}");
            }
        }

        _logger.LogInformation("Found {Count} theme files across {DirCount} directories",
            themePaths.Count, ThemeDirectoryPaths.Length);
        Console.WriteLine($"[ThemeDirectoryScanner] Found {themePaths.Count} theme files across {ThemeDirectoryPaths.Length} directories");
        System.Diagnostics.Debug.WriteLine($"[ThemeDirectoryScanner] Found {themePaths.Count} theme files across {ThemeDirectoryPaths.Length} directories");

        return themePaths;
    }

    private static async Task ScanDirectoryAsync(
        string directory,
        ISet<string> themePaths,
        CancellationToken cancellationToken)
    {
        try
        {
            var themeFiles = Directory.EnumerateFiles(directory, "*.theme", SearchOption.TopDirectoryOnly);

            foreach (var file in themeFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                themePaths.Add(file);
            }

            // Also check for .deskthemepack files (packaged themes)
            var packagedThemes = Directory.EnumerateFiles(directory, "*.deskthemepack", SearchOption.TopDirectoryOnly);
            foreach (var file in packagedThemes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                themePaths.Add(file);
            }

            // Await to allow cancellation checks
            await Task.Yield();
        }
        catch (UnauthorizedAccessException)
        {
            // Silently skip directories we can't access
        }
        catch (IOException)
        {
            // Silently skip directories with IO errors
        }
    }
}
