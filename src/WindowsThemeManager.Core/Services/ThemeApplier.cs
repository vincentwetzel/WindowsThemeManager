using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Helpers;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Applies themes using Windows APIs.
/// </summary>
public class ThemeApplier : IThemeApplier
{
    private readonly ILogger<ThemeApplier> _logger;

    public ThemeApplier(ILogger<ThemeApplier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> ApplyThemeAsync(Theme theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);

        _logger.LogInformation("Applying theme: {DisplayName}", theme.DisplayName);
        Console.WriteLine($"[ThemeApplier] Applying theme: {theme.DisplayName}");
        System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Applying theme: {theme.DisplayName}");

        try
        {
            // Strategy 1: Apply the .theme file directly via Windows shell
            // This is the most reliable method as Windows handles all components
            if (!string.IsNullOrEmpty(theme.ThemePath) && File.Exists(theme.ThemePath))
            {
                ThemeApplierNative.ApplyThemeByPath(theme.ThemePath);
                _logger.LogInformation("Theme file applied: {ThemePath}", theme.ThemePath);
                Console.WriteLine($"[ThemeApplier] Theme file applied: {theme.ThemePath}");
                System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Theme file applied: {theme.ThemePath}");
            }
            // Strategy 2: Apply individual components manually
            else
            {
                ApplyComponentsManually(theme);
            }

            // Broadcast the settings change
            ThemeApplierNative.BroadcastSettingsChange();

            // Small delay for the system to process
            Thread.Sleep(500);

            _logger.LogInformation("Theme applied successfully: {DisplayName}", theme.DisplayName);
            Console.WriteLine($"[ThemeApplier] Theme applied successfully: {theme.DisplayName}");
            System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Theme applied successfully: {theme.DisplayName}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme: {DisplayName}", theme.DisplayName);
            Console.WriteLine($"[ThemeApplier] Failed to apply theme: {theme.DisplayName} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Failed to apply theme: {theme.DisplayName} - {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> SetWallpaperAsync(string? wallpaperPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
            {
                _logger.LogWarning("Cannot set wallpaper - file not found: {Path}", wallpaperPath);
                Console.WriteLine($"[ThemeApplier] Cannot set wallpaper - file not found: {wallpaperPath}");
                System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Cannot set wallpaper - file not found: {wallpaperPath}");
                return Task.FromResult(false);
            }

            var result = ThemeApplierNative.SetWallpaper(wallpaperPath);

            if (result)
            {
                _logger.LogInformation("Wallpaper set: {Path}", wallpaperPath);
                Console.WriteLine($"[ThemeApplier] Wallpaper set: {wallpaperPath}");
                System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Wallpaper set: {wallpaperPath}");
            }
            else
            {
                _logger.LogWarning("Failed to set wallpaper: {Path} (error: {ErrorCode})",
                    wallpaperPath, Marshal.GetLastWin32Error());
                Console.WriteLine($"[ThemeApplier] Failed to set wallpaper: {wallpaperPath} (error: {Marshal.GetLastWin32Error()})");
                System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Failed to set wallpaper: {wallpaperPath} (error: {Marshal.GetLastWin32Error()})");
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting wallpaper: {Path}", wallpaperPath);
            Console.WriteLine($"[ThemeApplier] Error setting wallpaper: {wallpaperPath} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Error setting wallpaper: {wallpaperPath} - {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Applies individual theme components when no .theme file is available.
    /// </summary>
    private void ApplyComponentsManually(Theme theme)
    {
        // Apply wallpaper
        if (!string.IsNullOrEmpty(theme.WallpaperPath) && File.Exists(theme.WallpaperPath))
        {
            ThemeApplierNative.SetWallpaper(theme.WallpaperPath);
            _logger.LogDebug("Applied wallpaper: {Path}", theme.WallpaperPath);
            Console.WriteLine($"[ThemeApplier] Applied wallpaper: {theme.WallpaperPath}");
            System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Applied wallpaper: {theme.WallpaperPath}");
        }

        // Apply visual style
        if (!string.IsNullOrEmpty(theme.VisualStylePath) && File.Exists(theme.VisualStylePath))
        {
            ThemeApplierNative.ApplyVisualStyle(theme.VisualStylePath);
            _logger.LogDebug("Applied visual style: {Path}", theme.VisualStylePath);
            Console.WriteLine($"[ThemeApplier] Applied visual style: {theme.VisualStylePath}");
            System.Diagnostics.Debug.WriteLine($"[ThemeApplier] Applied visual style: {theme.VisualStylePath}");
        }

        // Sound scheme and cursor scheme require registry manipulation
        // These are complex and vary by system, so we skip them for now
        // TODO: Implement sound scheme application
        // TODO: Implement cursor scheme application
    }
}
