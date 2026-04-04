using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Services;
using System.Diagnostics;
using System.Windows.Media;

namespace WindowsThemeManager.ViewModels;

/// <summary>
/// ViewModel for a single monitor in the layout display.
/// </summary>
public partial class MonitorItemViewModel : ObservableObject
{
    private readonly MonitorInfo _monitorInfo;
    private readonly IWallpaperImageService _imageService;
    private ImageSource? _wallpaperPreview;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private int _monitorNumber;

    [ObservableProperty]
    private bool _isPrimary;

    [ObservableProperty]
    private string? _wallpaperPath;

    [ObservableProperty]
    private double _canvasLeft;

    [ObservableProperty]
    private double _canvasTop;

    [ObservableProperty]
    private double _canvasWidth;

    [ObservableProperty]
    private double _canvasHeight;

    /// <summary>
    /// The monitor bounds in virtual screen coordinates (for position calculation).
    /// </summary>
    public WindowsThemeManager.Core.Models.IntRect Bounds => _monitorInfo.Bounds;

    /// <summary>
    /// Wallpaper preview image. Uses object to avoid WPF type dependency in source-generated code.
    /// Bound directly to Image.Source in the View layer.
    /// </summary>
    public ImageSource? WallpaperPreview
    {
        get => _wallpaperPreview;
        set => SetProperty(ref _wallpaperPreview, value);
    }

    /// <summary>
    /// Tooltip text for the monitor.
    /// </summary>
    public string TooltipText => $"{DeviceName} ({_monitorInfo.Bounds.Width}x{_monitorInfo.Bounds.Height}){(IsPrimary ? " - Primary" : "")}";

    public MonitorItemViewModel(MonitorInfo monitorInfo, IWallpaperImageService imageService)
    {
        _monitorInfo = monitorInfo;
        _imageService = imageService;

        DeviceName = monitorInfo.DeviceName;
        MonitorNumber = monitorInfo.MonitorNumber;
        IsPrimary = monitorInfo.IsPrimary;
        WallpaperPath = monitorInfo.CurrentWallpaperPath;
    }

    /// <summary>
    /// Opens the wallpaper file in the default image viewer.
    /// </summary>
    [RelayCommand]
    private void OpenWallpaper()
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG-3] OpenWallpaper command executed");
        System.Diagnostics.Debug.WriteLine($"[DEBUG-3] WallpaperPath: {WallpaperPath ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG-3] IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(WallpaperPath)}");

        if (string.IsNullOrEmpty(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] ABORT: wallpaper path is null or empty");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] ABORT: file does not exist: {WallpaperPath}");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] Attempting Process.Start: {WallpaperPath}");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = WallpaperPath,
                UseShellExecute = true
            };
            var proc = System.Diagnostics.Process.Start(psi);
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] Process.Start returned: Id={proc?.Id ?? 0}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the wallpaper thumbnail for display.
    /// </summary>
    public async Task LoadWallpaperPreviewAsync(int maxWidth = 400, int maxHeight = 300)
    {
        if (string.IsNullOrEmpty(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor {MonitorNumber}] No wallpaper path set");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} has no wallpaper path");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor {MonitorNumber}] Wallpaper file not found: {WallpaperPath}");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} wallpaper file missing path={WallpaperPath}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[Monitor {MonitorNumber}] Loading wallpaper: {WallpaperPath}");
        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Loading monitor {MonitorNumber} wallpaper path={WallpaperPath}");
        // Clear the previous image first so the UI always sees a concrete transition.
        WallpaperPreview = null;
        WallpaperPreview = await _imageService.LoadThumbnailAsync(WallpaperPath, maxWidth, maxHeight);
        
        if (WallpaperPreview == null)
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor {MonitorNumber}] Failed to load wallpaper preview");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} thumbnail load returned null");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor {MonitorNumber}] Successfully loaded wallpaper preview");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} thumbnail load succeeded");
        }
    }

    /// <summary>
    /// Updates the canvas positioning based on the normalized layout.
    /// </summary>
    public void UpdateCanvasPosition(double left, double top, double width, double height)
    {
        CanvasLeft = left;
        CanvasTop = top;
        CanvasWidth = width;
        CanvasHeight = height;
    }
}
