using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;
using WindowsThemeManager.Themes;

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

        // Apply default theme immediately so all DynamicResource keys exist
        // before any DataTemplate is ever applied
        ThemeResources.ApplyTheme(isDark: false);
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
    private async void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            await settingsService.SaveAsync();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}







