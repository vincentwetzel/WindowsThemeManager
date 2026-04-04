using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WindowsThemeManager.Services;

/// <summary>
/// Loads and caches wallpaper images with thumbnail generation.
/// </summary>
public class WallpaperImageService : IWallpaperImageService
{
    private readonly ILogger<WallpaperImageService> _logger;
    private readonly ConcurrentDictionary<string, BitmapSource> _fullImageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, BitmapSource> _thumbnailCache = new(StringComparer.OrdinalIgnoreCase);

    public WallpaperImageService(ILogger<WallpaperImageService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public int CachedImageCount => _fullImageCache.Count;

    /// <inheritdoc />
    public Task<BitmapSource?> LoadWallpaperAsync(string? wallpaperPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
        {
            _logger.LogDebug("Wallpaper not found or path is empty: {Path}", wallpaperPath);
            Console.WriteLine($"[WallpaperImageService] Wallpaper not found or path is empty: {wallpaperPath}");
            System.Diagnostics.Debug.WriteLine($"[WallpaperImageService] Wallpaper not found or path is empty: {wallpaperPath}");
            return Task.FromResult<BitmapSource?>(null);
        }

        // Check cache first
        if (_fullImageCache.TryGetValue(wallpaperPath!, out var cached))
        {
            _logger.LogDebug("Full image cache hit: {Path}", wallpaperPath);
            Console.WriteLine($"[WallpaperImageService] Full image cache hit: {wallpaperPath}");
            Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Full image cache hit path={wallpaperPath}");
            return Task.FromResult<BitmapSource?>(cached);
        }

        try
        {
            var bitmap = LoadImageFromFile(wallpaperPath, decodePixelWidth: 0);
            _fullImageCache[wallpaperPath] = bitmap;

            _logger.LogDebug("Loaded wallpaper: {Path}", wallpaperPath);
            Console.WriteLine($"[WallpaperImageService] Loaded wallpaper: {wallpaperPath}");
            Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Loaded full image path={wallpaperPath}");
            return Task.FromResult<BitmapSource?>(bitmap);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load wallpaper: {Path}", wallpaperPath);
            Console.WriteLine($"[WallpaperImageService] Failed to load wallpaper: {wallpaperPath} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[WallpaperImageService] Failed to load wallpaper: {wallpaperPath} - {ex.Message}");
            return Task.FromResult<BitmapSource?>(null);
        }
    }

    /// <inheritdoc />
    public Task<BitmapSource?> LoadThumbnailAsync(string? wallpaperPath, int maxWidth, int maxHeight, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
        {
            return Task.FromResult<BitmapSource?>(null);
        }

        // Create a cache key that includes the size
        var cacheKey = $"{wallpaperPath}|{maxWidth}x{maxHeight}";

        // Check cache first
        if (_thumbnailCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Thumbnail cache hit: {Key}", cacheKey);
            Console.WriteLine($"[WallpaperImageService] Thumbnail cache hit: {cacheKey}");
            Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Thumbnail cache hit key={cacheKey}");
            return Task.FromResult<BitmapSource?>(cached);
        }

        try
        {
            var thumbnail = LoadImageFromFile(wallpaperPath, maxWidth);
            _thumbnailCache[cacheKey] = thumbnail;

            _logger.LogDebug("Generated thumbnail: {Path} ({Width}x{Height})", wallpaperPath, maxWidth, maxHeight);
            Console.WriteLine($"[WallpaperImageService] Generated thumbnail: {wallpaperPath} ({maxWidth}x{maxHeight})");
            Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Generated thumbnail path={wallpaperPath} size={maxWidth}x{maxHeight}");
            return Task.FromResult<BitmapSource?>(thumbnail);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail: {Path}", wallpaperPath);
            Console.WriteLine($"[WallpaperImageService] Failed to generate thumbnail: {wallpaperPath} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[WallpaperImageService] Failed to generate thumbnail: {wallpaperPath} - {ex.Message}");
            return Task.FromResult<BitmapSource?>(null);
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _fullImageCache.Clear();
        _thumbnailCache.Clear();
        _logger.LogInformation("Wallpaper image cache cleared");
        Console.WriteLine("[WallpaperImageService] Wallpaper image cache cleared");
        Trace.WriteLine($"[{DateTime.Now:O}] [WallpaperImageService] Cache cleared");
    }

    private static BitmapSource LoadImageFromFile(string filePath, int decodePixelWidth = 0)
    {
        var bitmap = new BitmapImage();
        
        // Read the file into a byte array first to avoid file locking issues
        var fileBytes = File.ReadAllBytes(filePath);
        
        using var stream = new MemoryStream(fileBytes);
        
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;

        // Set decode width for thumbnail generation (reduces memory)
        if (decodePixelWidth > 0)
        {
            bitmap.DecodePixelWidth = decodePixelWidth;
        }

        bitmap.EndInit();
        bitmap.Freeze(); // Make it cross-thread accessible

        return bitmap;
    }
}
