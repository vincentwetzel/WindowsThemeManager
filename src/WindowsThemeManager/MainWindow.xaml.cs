using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;
using WindowsThemeManager.Themes;
using WindowsThemeManager.ViewModels;

namespace WindowsThemeManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
{
    private bool _isDark;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark == value) return;
            _isDark = value;
            OnPropertyChanged();
            ApplyThemeColors();
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyThemeColors();
    }

    /// <summary>
    /// Sets the theme mode combobox and applies theme.
    /// Call this after the DataContext is set.
    /// </summary>
    public void SetThemeMode(AppThemeMode mode)
    {
        if (ThemeModeComboBox == null)
        {
            System.Diagnostics.Trace.WriteLine($"[{DateTime.Now:O}] [MainWindow] ThemeModeComboBox is null in SetThemeMode");
            return;
        }

        ThemeModeComboBox.SelectedIndex = mode switch
        {
            AppThemeMode.System => 0,
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0
        };

        IsDark = Services.ThemeManager.GetEffectiveTheme(mode) == AppThemeMode.Dark;
    }

    /// <summary>
    /// Applies the current theme by updating App-level resource brushes.
    /// All XAML elements use DynamicResource references to these brushes,
    /// so changing them here updates the entire app UI in one place.
    /// </summary>
    private void ApplyThemeColors()
    {
        ThemeResources.ApplyTheme(IsDark);
    }

    /// <summary>
    /// Handles theme mode selection change.
    /// </summary>
    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeModeComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;

        var mode = selectedItem.Content.ToString() switch
        {
            "System" => AppThemeMode.System,
            "Light" => AppThemeMode.Light,
            "Dark" => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };

        IsDark = Services.ThemeManager.GetEffectiveTheme(mode) == AppThemeMode.Dark;

        // Persist the setting
        var sp = App.Services;
        if (sp != null)
        {
            var settingsService = sp.GetRequiredService<SettingsService>();
            settingsService.Settings.ThemeMode = mode;
            settingsService.SaveAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Window-level PreviewMouseDown. Uses coordinate-based hit testing on the Canvas.
    /// </summary>
    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm)
            return;

        if (mainVm.Monitors.Count == 0)
            return;

        var canvas = FindVisualChild<Canvas>(MonitorsItemsControl);
        if (canvas == null)
            return;

        var clickPoint = e.GetPosition(canvas);

        foreach (var monitor in mainVm.Monitors)
        {
            double left = monitor.CanvasLeft;
            double top = monitor.CanvasTop;
            double right = left + monitor.CanvasWidth;
            double bottom = top + monitor.CanvasHeight;

            if (clickPoint.X >= left && clickPoint.X <= right &&
                clickPoint.Y >= top && clickPoint.Y <= bottom)
            {
                if (!string.IsNullOrEmpty(monitor.WallpaperPath) && System.IO.File.Exists(monitor.WallpaperPath))
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = monitor.WallpaperPath,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to open wallpaper: {ex.Message}");
                    }
                }

                e.Handled = true;
                return;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
