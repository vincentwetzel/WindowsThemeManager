using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Core.Services;
using WindowsThemeManager.Services;

namespace WindowsThemeManager.ViewModels;

/// <summary>
/// Main application ViewModel that orchestrates theme browsing and monitor display.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private static readonly TimeSpan WallpaperRefreshDelay = TimeSpan.FromMilliseconds(250);
    private readonly IThemeService _themeService;
    private readonly IMonitorService _monitorService;
    private readonly IWallpaperImageService _imageService;
    private readonly SettingsService _settings;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly object _wallpaperRefreshGate = new();
    private CancellationTokenSource? _wallpaperRefreshCts;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _themePanelWidth = 300;

    /// <summary>
    /// Collection of available themes.
    /// </summary>
    public ObservableCollection<ThemeItemViewModel> Themes { get; } = new();

    /// <summary>
    /// Collection of monitor view models for the layout display.
    /// </summary>
    public ObservableCollection<MonitorItemViewModel> Monitors { get; } = new();

    /// <summary>
    /// The currently selected theme.
    /// </summary>
    [ObservableProperty]
    private ThemeItemViewModel? _selectedTheme;

    public MainViewModel(
        IThemeService themeService,
        IMonitorService monitorService,
        IWallpaperImageService imageService,
        SettingsService settings,
        IDialogService dialogService,
        ILogger<MainViewModel> logger)
    {
        _themeService = themeService;
        _monitorService = monitorService;
        _imageService = imageService;
        _settings = settings;
        _dialogService = dialogService;
        _logger = logger;

        ThemePanelWidth = settings.Settings.ThemePanelWidth;
        _themeService.ThemeChanged += OnThemeChanged;
        _monitorService.WallpaperChanged += OnWallpaperChanged;

        // Start listening for wallpaper changes
        _monitorService.StartListeningForWallpaperChanges();
    }

    /// <summary>
    /// Loads all themes and monitor information on startup.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        StatusMessage = "Loading themes and detecting monitors...";

        try
        {
            // Load themes and monitors in parallel
            var themesTask = _themeService.DiscoverThemesAsync();
            var layoutTask = _monitorService.GetMonitorLayoutAsync();

            await Task.WhenAll(themesTask, layoutTask);

            var themes = await themesTask;
            var layout = await layoutTask;

            // Update themes collection
            Application.Current.Dispatcher.Invoke(() =>
            {
                Themes.Clear();
                foreach (var theme in themes)
                {
                    var vm = new ThemeItemViewModel(theme, _themeService);
                    Themes.Add(vm);
                }
            });

            // Update monitors collection
            Application.Current.Dispatcher.Invoke(() =>
            {
                Monitors.Clear();
                foreach (var monitor in layout.Monitors)
                {
                    var vm = new MonitorItemViewModel(monitor, _imageService);
                    Monitors.Add(vm);
                }
            });

            // Calculate monitor layout positions
            CalculateMonitorPositions(layout);

            // Load wallpaper previews for all monitors
            await LoadMonitorPreviewsAsync();

            // Try to detect and highlight current theme
            var currentTheme = await _themeService.GetCurrentThemeAsync();
            if (currentTheme != null)
            {
                var currentVm = Themes.FirstOrDefault(t => t.Theme.ThemePath == currentTheme.ThemePath);
                if (currentVm != null)
                {
                    currentVm.IsActive = true;
                    SelectedTheme = currentVm;
                }
            }

            StatusMessage = $"Loaded {Themes.Count} themes, {Monitors.Count} monitor(s)";
            _logger.LogInformation("Loaded {ThemeCount} themes and {MonitorCount} monitors",
                Themes.Count, Monitors.Count);
            Console.WriteLine($"[MainViewModel] Loaded {Themes.Count} themes and {Monitors.Count} monitors");
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Loaded {Themes.Count} themes and {Monitors.Count} monitors");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading: {ex.Message}";
            _logger.LogError(ex, "Failed to load themes and monitors");
            Console.WriteLine($"[MainViewModel] Failed to load themes and monitors: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to load themes and monitors: {ex.Message}");
            _dialogService.ShowError(
                $"Failed to load themes and monitors.\n\nDetails: {ex.Message}\n\nCheck the debug output for more information.",
                "Load Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes themes and monitors.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        await _themeService.RefreshThemesAsync();
        await LoadAsync();
    }

    /// <summary>
    /// Calculates normalized canvas positions for monitors.
    /// </summary>
    private void CalculateMonitorPositions(MonitorLayout layout)
    {
        if (layout.TotalBounds.IsEmpty || Monitors.Count == 0)
            return;

        // Define a standard canvas size to fit monitors into
        const double canvasWidth = 800;
        const double canvasHeight = 600;

        var scale = Math.Min(
            canvasWidth / layout.TotalBounds.Width,
            canvasHeight / layout.TotalBounds.Height);

        var offsetX = -layout.TotalBounds.X * scale;
        var offsetY = -layout.TotalBounds.Y * scale;

        foreach (var monitor in Monitors)
        {
            var bounds = monitor.Bounds;
            monitor.UpdateCanvasPosition(
                left: bounds.X * scale + offsetX,
                top: bounds.Y * scale + offsetY,
                width: bounds.Width * scale,
                height: bounds.Height * scale);
        }
    }

    /// <summary>
    /// Loads wallpaper previews for all monitors.
    /// </summary>
    private async Task LoadMonitorPreviewsAsync()
    {
        var tasks = Monitors.Select(m => m.LoadWallpaperPreviewAsync(400, 300));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Handles theme changed event from the service.
    /// </summary>
    private async void OnThemeChanged(object? sender, Theme e)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            // Update active state
            foreach (var theme in Themes)
            {
                theme.IsActive = theme.Theme.ThemePath == e.ThemePath;
            }

            // Reload monitor wallpapers to reflect the new theme
            await ReloadMonitorPreviewsAsync();

            StatusMessage = $"Applied theme: {e.DisplayName}";
        });
    }

    /// <summary>
    /// Handles wallpaper change events from the monitor service.
    /// Updates the affected monitor's wallpaper preview in real-time.
    /// </summary>
    private async void OnWallpaperChanged(object? sender, (string DevicePath, string? WallpaperPath) e)
    {
        System.Diagnostics.Debug.WriteLine($"[MainViewModel] Received wallpaper change event - DevicePath: {e.DevicePath}, WallpaperPath: {e.WallpaperPath ?? "(null)"}");
        Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] OnWallpaperChanged device={e.DevicePath} wallpaper={e.WallpaperPath ?? "(null)"}");

        CancellationTokenSource cts;
        lock (_wallpaperRefreshGate)
        {
            _wallpaperRefreshCts?.Cancel();
            _wallpaperRefreshCts?.Dispose();
            _wallpaperRefreshCts = new CancellationTokenSource();
            cts = _wallpaperRefreshCts;
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] Scheduled wallpaper refresh delay={WallpaperRefreshDelay.TotalMilliseconds}ms");
        }

        try
        {
            await Task.Delay(WallpaperRefreshDelay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] Wallpaper refresh canceled before reload");
            return;
        }

        if (cts.IsCancellationRequested)
            return;

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            // Slideshow advances can affect the rendered image for any monitor, so refresh the full set
            // and invalidate cached bitmaps instead of trying to update only one item from the event payload.
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] Dispatcher refresh started");
            _imageService.ClearCache();
            await ReloadMonitorPreviewsAsync();
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] Dispatcher refresh completed");

            StatusMessage = string.IsNullOrEmpty(e.DevicePath)
                ? "Wallpaper updated"
                : $"Wallpaper updated on {e.DevicePath}";
        });
    }

    /// <summary>
    /// Reloads wallpaper previews for all monitors (after theme change).
    /// </summary>
    private async Task ReloadMonitorPreviewsAsync()
    {
        try
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] ReloadMonitorPreviewsAsync start monitors={Monitors.Count}");
            var layout = await _monitorService.GetMonitorLayoutAsync();
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] ReloadMonitorPreviewsAsync layout monitors={layout.Monitors.Count}");

            for (int i = 0; i < Monitors.Count && i < layout.Monitors.Count; i++)
            {
                var monitor = Monitors[i];
                var newWallpaper = layout.Monitors[i].CurrentWallpaperPath;
                Trace.WriteLine(
                    $"[{DateTime.Now:O}] [MainViewModel] Reloading monitor {monitor.MonitorNumber} device={monitor.DeviceName} old={monitor.WallpaperPath ?? "(null)"} new={newWallpaper ?? "(null)"}");
                monitor.WallpaperPath = newWallpaper;
                await monitor.LoadWallpaperPreviewAsync(400, 300);
            }

            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] ReloadMonitorPreviewsAsync complete");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reload monitor previews after theme change");
            Trace.WriteLine($"[{DateTime.Now:O}] [MainViewModel] ReloadMonitorPreviewsAsync exception: {ex}");
        }
    }
}
