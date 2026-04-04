using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.ViewModels;

/// <summary>
/// ViewModel for a single theme item in the theme list.
/// </summary>
public partial class ThemeItemViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private Theme _theme;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isApplying;

    public ThemeItemViewModel(Theme theme, IThemeService themeService)
    {
        _theme = theme;
        _themeService = themeService;
    }

    /// <summary>
    /// Applies this theme when the user clicks on it.
    /// </summary>
    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (IsApplying || IsActive)
            return;

        IsApplying = true;

        try
        {
            await _themeService.ApplyThemeAsync(Theme);
            IsActive = true;
        }
        finally
        {
            IsApplying = false;
        }
    }

    public void UpdateFromTheme(Theme theme)
    {
        Theme = theme;
    }
}
