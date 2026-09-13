using System.Windows.Media.Imaging;

namespace WindowsThemeManager.Services;

/// <summary>
/// Service for loading and caching wallpaper thumbnails.
/// </summary>
public interface IWallpaperImageService
{
    /// <summary>
    /// Loads an image suitable for display on a monitor preview.
    /// </summary>
    Task<BitmapSource?> LoadThumbnailAsync(string? wallpaperPath);

    /// <summary>
    /// Clears the image cache.
    /// </summary>
    void ClearCache();
}
