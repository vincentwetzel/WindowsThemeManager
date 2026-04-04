using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WindowsThemeManager.Services;

/// <summary>
/// Service for loading and caching wallpaper images and thumbnails.
/// </summary>
public interface IWallpaperImageService
{
    /// <summary>
    /// Loads a wallpaper image from a file path.
    /// </summary>
    Task<BitmapSource?> LoadWallpaperAsync(string? wallpaperPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thumbnail suitable for display on a monitor preview.
    /// </summary>
    Task<BitmapSource?> LoadThumbnailAsync(string? wallpaperPath, int maxWidth, int maxHeight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the image cache.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets the number of cached images.
    /// </summary>
    int CachedImageCount { get; }
}
