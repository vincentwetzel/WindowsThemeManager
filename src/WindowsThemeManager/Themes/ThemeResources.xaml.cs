using System.Windows;
using System.Windows.Media;

namespace WindowsThemeManager.Themes;

/// <summary>
/// Code-behind for ThemeResources.xaml — provides theme brush swapping.
/// </summary>
public partial class ThemeResources : ResourceDictionary
{
    private static readonly string[] BrushKeys = {
        "AppBackground", "AppSurface", "AppSurfaceAlt",
        "AppTextPrimary", "AppTextSecondary", "AppGridSplitter",
        "AppLoadingOverlay", "AppMonitorHeaderText", "AppMonitorBg",
        "AppMonitorBorder", "AppMonitorHoverBorder", "AppMonitorPlaceholder",
        "AppMonitorBadgeBg", "AppMonitorBadgeText", "AppLoadingIcon", "AppLoadingText"
    };

    public ThemeResources()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Applies the appropriate brushes to the application's resource dictionary.
    /// </summary>
    public static void ApplyTheme(bool isDark)
    {
        var resources = Application.Current.Resources;
        var suffix = isDark ? ".Dark" : ".Light";

        foreach (var key in BrushKeys)
        {
            var sourceKey = key + suffix;
            if (resources[sourceKey] is Brush brush)
            {
                resources[key] = brush;
            }
        }
    }
}
