using System.Diagnostics;
using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace WindowsThemeManager.Services;

/// <summary>
/// Loads and caches wallpaper thumbnails.
/// </summary>
public class WallpaperImageService : IWallpaperImageService
{
    private readonly ILogger<WallpaperImageService> _logger;
    private readonly ConcurrentDictionary<string, BitmapSource> _thumbnailCache = new(StringComparer.OrdinalIgnoreCase);

    public WallpaperImageService(ILogger<WallpaperImageService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<BitmapSource?> LoadThumbnailAsync(string? wallpaperPath)
    {
        if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
        {
            return Task.FromResult<BitmapSource?>(null);
        }

        var cacheKey = wallpaperPath;

        // Check cache first
        if (_thumbnailCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Thumbnail cache hit: {Key}", cacheKey);
            Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Thumbnail cache hit key={cacheKey}");
            return Task.FromResult<BitmapSource?>(cached);
        }

        try
        {
            // Keep the original decoded dimensions. Some valid Windows wallpaper
            // formats fail when BitmapImage.DecodePixelWidth is applied (especially
            // to transcoded/slideshow cache files). The WPF Image control performs
            // the visual scaling via Stretch, so a reduced decode is not required
            // for the monitor preview.
            var thumbnail = LoadImageFromFile(wallpaperPath);
            _thumbnailCache[cacheKey] = thumbnail;

            _logger.LogDebug("Generated thumbnail: {Path}", wallpaperPath);
            Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Generated thumbnail path={wallpaperPath}");
            return Task.FromResult<BitmapSource?>(thumbnail);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail: {Path}", wallpaperPath);
            return Task.FromResult<BitmapSource?>(null);
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _thumbnailCache.Clear();
        _logger.LogInformation("Wallpaper image cache cleared");
        Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Cache cleared");
    }

    private static BitmapSource LoadImageFromFile(string filePath)
    {
        var bitmap = new BitmapImage();
        
        // Read the file into a byte array first to avoid file locking issues
        var fileBytes = File.ReadAllBytes(filePath);
        
        using var stream = new MemoryStream(fileBytes);
        
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;

        bitmap.EndInit();
        bitmap.Freeze(); // Make it cross-thread accessible

        return bitmap;
    }
}
