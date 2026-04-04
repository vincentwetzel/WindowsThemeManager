using System.Windows;
using System.Windows.Media;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Themes;

/// <summary>
/// Centralized application theme color definitions.
/// All app colors are defined here — no hardcoded colors elsewhere in the app.
/// </summary>
public static class AppThemeColors
{
    // ── Light palette ──
    public static Color LightBackground       => Color.FromRgb(0xF5, 0xF5, 0xF5);
    public static Color LightSurface           => Color.FromRgb(0xE8, 0xE8, 0xE8);
    public static Color LightSurfaceAlt        => Color.FromRgb(0x1E, 0x1E, 0x1E);
    public static Color LightTextPrimary       => Color.FromRgb(0x00, 0x00, 0x00);
    public static Color LightTextSecondary     => Color.FromRgb(0x80, 0x80, 0x80);
    public static Color LightGridSplitter      => Color.FromRgb(0xCC, 0xCC, 0xCC);
    public static Color LightLoadingOverlay    => Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
    public static Color LightMonitorHeaderText => Colors.White;       // monitor area always has white text on dark bg
    public static Color LightMonitorBg         => Color.FromRgb(0x1E, 0x1E, 0x1E);
    public static Color LightMonitorBorder     => Color.FromRgb(0x55, 0x55, 0x55);
    public static Color LightMonitorHoverBorder=> Color.FromRgb(0x00, 0x78, 0xD4);
    public static Color LightMonitorPlaceholder=> Color.FromRgb(0x3A, 0x3A, 0x3A);
    public static Color LightMonitorBadgeBg    => Color.FromArgb(0xAA, 0x00, 0x00, 0x00);
    public static Color LightMonitorBadgeText  => Colors.White;
    public static Color LightLoadingIcon       => Colors.Black;
    public static Color LightLoadingText       => Color.FromRgb(0x33, 0x33, 0x33);

    // ── Dark palette ──
    public static Color DarkBackground        => Color.FromRgb(0x2D, 0x2D, 0x2D);
    public static Color DarkSurface            => Color.FromRgb(0x25, 0x25, 0x25);
    public static Color DarkSurfaceAlt         => Color.FromRgb(0x1A, 0x1A, 0x1A);
    public static Color DarkTextPrimary        => Color.FromRgb(0xE0, 0xE0, 0xE0);
    public static Color DarkTextSecondary      => Color.FromRgb(0x99, 0x99, 0x99);
    public static Color DarkGridSplitter       => Color.FromRgb(0x40, 0x40, 0x40);
    public static Color DarkLoadingOverlay     => Color.FromArgb(0x80, 0x1E, 0x1E, 0x1E);
    public static Color DarkMonitorHeaderText  => Color.FromRgb(0xE0, 0xE0, 0xE0);
    public static Color DarkMonitorBg          => Color.FromRgb(0x1A, 0x1A, 0x1A);
    public static Color DarkMonitorBorder      => Color.FromRgb(0x44, 0x44, 0x44);
    public static Color DarkMonitorHoverBorder => Color.FromRgb(0x00, 0x78, 0xD4);
    public static Color DarkMonitorPlaceholder => Color.FromRgb(0x2A, 0x2A, 0x2A);
    public static Color DarkMonitorBadgeBg     => Color.FromArgb(0xAA, 0x00, 0x00, 0x00);
    public static Color DarkMonitorBadgeText   => Colors.White;
    public static Color DarkLoadingIcon        => Color.FromRgb(0xE0, 0xE0, 0xE0);
    public static Color DarkLoadingText        => Color.FromRgb(0xE0, 0xE0, 0xE0);

    // ── Shared (theme-independent) ──
    public static Color AccentBackground       => Color.FromRgb(0xE3, 0xF2, 0xFD);  // active theme highlight bg
    public static Color AccentBorder           => Color.FromRgb(0x19, 0x76, 0xD2);   // active theme highlight border

    /// <summary>
    /// Returns the complete palette for the given theme mode.
    /// </summary>
    public static ThemePalette GetPalette(AppThemeMode mode)
    {
        bool isDark = mode == AppThemeMode.Dark ||
            (mode == AppThemeMode.System && Services.ThemeManager.GetEffectiveTheme(AppThemeMode.System) == AppThemeMode.Dark);
        return isDark ? DarkPalette : LightPalette;
    }

    public static ThemePalette LightPalette { get; } = new(
        Background:       LightBackground,
        Surface:          LightSurface,
        SurfaceAlt:       LightSurfaceAlt,
        TextPrimary:      LightTextPrimary,
        TextSecondary:    LightTextSecondary,
        GridSplitter:     LightGridSplitter,
        LoadingOverlay:   LightLoadingOverlay,
        MonitorHeaderText:LightMonitorHeaderText,
        MonitorBg:        LightMonitorBg,
        MonitorBorder:    LightMonitorBorder,
        MonitorHoverBorder: LightMonitorHoverBorder,
        MonitorPlaceholder: LightMonitorPlaceholder,
        MonitorBadgeBg:  LightMonitorBadgeBg,
        MonitorBadgeText:LightMonitorBadgeText,
        LoadingIcon:     LightLoadingIcon,
        LoadingText:     LightLoadingText
    );

    public static ThemePalette DarkPalette { get; } = new(
        Background:       DarkBackground,
        Surface:          DarkSurface,
        SurfaceAlt:       DarkSurfaceAlt,
        TextPrimary:      DarkTextPrimary,
        TextSecondary:    DarkTextSecondary,
        GridSplitter:     DarkGridSplitter,
        LoadingOverlay:   DarkLoadingOverlay,
        MonitorHeaderText:DarkMonitorHeaderText,
        MonitorBg:        DarkMonitorBg,
        MonitorBorder:    DarkMonitorBorder,
        MonitorHoverBorder: DarkMonitorHoverBorder,
        MonitorPlaceholder: DarkMonitorPlaceholder,
        MonitorBadgeBg:  DarkMonitorBadgeBg,
        MonitorBadgeText:DarkMonitorBadgeText,
        LoadingIcon:     DarkLoadingIcon,
        LoadingText:     DarkLoadingText
    );
}

/// <summary>
/// Immutable snapshot of all colors for the current theme.
/// </summary>
public record ThemePalette(
    Color Background,
    Color Surface,
    Color SurfaceAlt,
    Color TextPrimary,
    Color TextSecondary,
    Color GridSplitter,
    Color LoadingOverlay,
    Color MonitorHeaderText,
    Color MonitorBg,
    Color MonitorBorder,
    Color MonitorHoverBorder,
    Color MonitorPlaceholder,
    Color MonitorBadgeBg,
    Color MonitorBadgeText,
    Color LoadingIcon,
    Color LoadingText
);
