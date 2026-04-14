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
    private bool _themeApplied;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;

        // Apply default theme immediately so all DynamicResource keys exist
        // before any DataTemplate is ever applied
        ThemeResources.ApplyTheme(isDark: false);
    }

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark == value && _themeApplied) return;
            _isDark = value;
            _themeApplied = true;
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

    private void MonitorPreviewBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MonitorItemViewModel monitor)
        {
            if (monitor.OpenWallpaperCommand.CanExecute(null))
            {
                monitor.OpenWallpaperCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void DeleteWallpaperButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MonitorItemViewModel monitor)
        {
            if (monitor.DeleteWallpaperCommand.CanExecute(null))
            {
                monitor.DeleteWallpaperCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void MonitorAreaGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm)
        {
            return;
        }

        var clickPoint = e.GetPosition(MonitorsItemsControl);
        mainVm.StatusMessage = $"Monitor area click at ({clickPoint.X:F0}, {clickPoint.Y:F0})";

        foreach (var monitor in mainVm.Monitors)
        {
            var left = monitor.CanvasLeft;
            var top = monitor.CanvasTop;
            var right = left + monitor.CanvasWidth;
            var bottom = top + monitor.CanvasHeight;

            if (clickPoint.X < left || clickPoint.X > right || clickPoint.Y < top || clickPoint.Y > bottom)
            {
                continue;
            }

            var deleteLeft = right - 40;
            var deleteTop = top + 8;
            var deleteRight = right - 8;
            var deleteBottom = top + 40;

            if (clickPoint.X >= deleteLeft && clickPoint.X <= deleteRight &&
                clickPoint.Y >= deleteTop && clickPoint.Y <= deleteBottom)
            {
                mainVm.StatusMessage = $"Clicked delete on monitor {monitor.MonitorNumber}";

                if (monitor.DeleteWallpaperCommand.CanExecute(null))
                {
                    monitor.DeleteWallpaperCommand.Execute(null);
                    e.Handled = true;
                }
                else
                {
                    mainVm.StatusMessage = $"Delete command not available for monitor {monitor.MonitorNumber}";
                }

                return;
            }

            mainVm.StatusMessage = $"Clicked preview on monitor {monitor.MonitorNumber}";

            if (monitor.OpenWallpaperCommand.CanExecute(null))
            {
                monitor.OpenWallpaperCommand.Execute(null);
                e.Handled = true;
            }
            else
            {
                mainVm.StatusMessage = $"Open command not available for monitor {monitor.MonitorNumber}";
            }

            return;
        }

        mainVm.StatusMessage = $"Monitor area click missed all monitor bounds at ({clickPoint.X:F0}, {clickPoint.Y:F0})";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}







