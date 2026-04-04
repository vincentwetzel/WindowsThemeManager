using System.Windows;

namespace WindowsThemeManager.Themes;

/// <summary>
/// Code-behind for ThemeResources.xaml.
/// Provides a helper to swap light/dark brushes into the app-level resource dictionary.
/// </summary>
public partial class ThemeResources : ResourceDictionary
{
    public ThemeResources()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Applies the appropriate brushes to the application's merged dictionaries.
    /// Copies the light or dark variants into App.Current.Resources so all XAML StaticResource references resolve.
    /// </summary>
    public static void ApplyTheme(bool isDark)
    {
        var resources = Application.Current.Resources;
        var suffix = isDark ? ".Dark" : ".Light";
        var lightSuffix = isDark ? ".Light" : ".Dark";

        string[] brushKeys = {
            "AppBackground", "AppSurface", "AppSurfaceAlt",
            "AppTextPrimary", "AppTextSecondary", "AppGridSplitter",
            "AppLoadingOverlay", "AppMonitorHeaderText", "AppMonitorBg",
            "AppMonitorBorder", "AppMonitorHoverBorder", "AppMonitorPlaceholder",
            "AppMonitorBadgeBg", "AppMonitorBadgeText", "AppLoadingIcon", "AppLoadingText"
        };

        foreach (var key in brushKeys)
        {
            var sourceKey = key + suffix;
            if (resources[sourceKey] is System.Windows.Media.Brush brush)
            {
                resources[key] = brush;
            }
        }
    }
}
