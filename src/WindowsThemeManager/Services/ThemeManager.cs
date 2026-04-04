using Microsoft.Win32;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Services;

/// <summary>
/// Static helper for theme mode detection.
/// </summary>
public static class ThemeManager
{
    /// <summary>
    /// Determines the effective theme based on the system setting if mode is System.
    /// </summary>
    public static AppThemeMode GetEffectiveTheme(AppThemeMode mode)
    {
        if (mode == AppThemeMode.System)
        {
            return IsWindowsDarkMode() ? AppThemeMode.Dark : AppThemeMode.Light;
        }
        return mode;
    }

    /// <summary>
    /// Checks if Windows is in dark mode.
    /// </summary>
    private static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value != null && (int)value == 0;
        }
        catch
        {
            return false;
        }
    }
}
