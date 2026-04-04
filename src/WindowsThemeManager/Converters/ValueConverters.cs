using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WindowsThemeManager.Themes;

namespace WindowsThemeManager.Converters;

/// <summary>
/// Converts boolean to visibility (true = Visible, false = Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return System.Windows.Visibility.Visible;
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Windows.Visibility v)
            return v == System.Windows.Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Returns a highlight brush for active theme items.
/// Uses the centralized accent color from AppThemeColors.
/// </summary>
public class ActiveThemeBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            var brush = new SolidColorBrush(AppThemeColors.AccentBackground);
            brush.Freeze();
            return brush;
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns a border brush for active theme items.
/// Uses the centralized accent color from AppThemeColors.
/// </summary>
public class ActiveThemeBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            var brush = new SolidColorBrush(AppThemeColors.AccentBorder);
            brush.Freeze();
            return brush;
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean to an icon character.
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return "🖥️";
        return "🎨";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts object to visibility (null/empty = Visible, has value = Collapsed).
/// Used for showing placeholders when values are missing.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;

        // Check for inverse parameter
        bool inverse = parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

        if (inverse)
            isNull = !isNull;

        return isNull ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts double to GridLength and back for column width bindings.
/// </summary>
public class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return new System.Windows.GridLength(d);
        return new System.Windows.GridLength(300);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Windows.GridLength gl)
            return gl.Value;
        return 300.0;
    }
}
