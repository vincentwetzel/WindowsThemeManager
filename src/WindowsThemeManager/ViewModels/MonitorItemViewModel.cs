using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace WindowsThemeManager.ViewModels;

/// <summary>
/// ViewModel for a single monitor in the layout display.
/// </summary>
public partial class MonitorItemViewModel : ObservableObject
{
    private readonly MonitorInfo _monitorInfo;
    private readonly IWallpaperImageService _imageService;
    private readonly IDialogService _dialogService;
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

    public MonitorItemViewModel(MonitorInfo monitorInfo, IWallpaperImageService imageService, IDialogService dialogService)
    {
        _monitorInfo = monitorInfo;
        _imageService = imageService;
        _dialogService = dialogService;

        DeviceName = monitorInfo.DeviceName;
        MonitorNumber = monitorInfo.MonitorNumber;
        IsPrimary = monitorInfo.IsPrimary;
        WallpaperPath = monitorInfo.CurrentWallpaperPath;
    }

    /// <summary>
    /// Deletes the current wallpaper on this monitor, moving it to the recycle bin.
    /// </summary>
    [RelayCommand]
    private async Task DeleteWallpaperAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG-3] DeleteWallpaper command executed");
        System.Diagnostics.Debug.WriteLine($"[DEBUG-3] WallpaperPath: {WallpaperPath ?? "null"}");

        if (string.IsNullOrEmpty(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] ABORT: wallpaper path is null or empty");
            _dialogService.ShowWarning($"No wallpaper file is associated with monitor {MonitorNumber}.", "Open Wallpaper");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] ABORT: file does not exist: {WallpaperPath}");
            _dialogService.ShowError($"The wallpaper file no longer exists:\n{WallpaperPath}", "Delete Error");
            return;
        }

        // Show confirmation dialog
        var fileName = Path.GetFileName(WallpaperPath);
        var confirmed = _dialogService.ShowConfirmation(
            $"Do you want to move the wallpaper for monitor {MonitorNumber} to the Recycle Bin?\n\n" +
            $"File: {fileName}\n" +
            $"Path: {WallpaperPath}\n\n" +
            $"This will send the file to the Recycle Bin.",
            "Move Wallpaper to Recycle Bin");

        if (!confirmed)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] User canceled delete operation");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] Attempting to delete wallpaper to recycle bin: {WallpaperPath}");
            
            // Move file to recycle bin using SHFileOperation
            bool success = MoveToRecycleBin(WallpaperPath);
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG-3] Successfully moved wallpaper to recycle bin");
                WallpaperPath = null;
                WallpaperPreview = null;
                _dialogService.ShowInfo(
                    $"Wallpaper has been moved to the Recycle Bin.\n\nFile: {fileName}",
                    "Wallpaper Deleted");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG-3] Failed to move wallpaper to recycle bin");
                _dialogService.ShowError(
                    $"Failed to move the wallpaper to the Recycle Bin.\n\nFile: {fileName}",
                    "Delete Error");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            _dialogService.ShowError(
                $"An error occurred while deleting the wallpaper:\n\n{ex.Message}",
                "Delete Error");
        }
    }

    /// <summary>
    /// Moves a file to the recycle bin using SHFileOperation API.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>True if successful, false otherwise.</returns>
    private static bool MoveToRecycleBin(string filePath)
    {
        try
        {
            // SHFileOperation requires double-null terminated strings
            string pFrom = filePath + '\0' + '\0';
            
            SHFILEOPSTRUCT fileop = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = pFrom,
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
            };

            int result = SHFileOperation(ref fileop);
            return result == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MoveToRecycleBin] Exception: {ex.Message}");
            return false;
        }
    }

    // P/Invoke declarations for SHFileOperation
    private const int FO_DELETE = 0x0003;
    private const int FOF_ALLOWUNDO = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;
    private const int FOF_SILENT = 0x0004;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszProgressTitle;
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
            _dialogService.ShowWarning($"No wallpaper file is associated with monitor {MonitorNumber}.", "Open Wallpaper");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG-3] ABORT: file does not exist: {WallpaperPath}");
            _dialogService.ShowError($"The wallpaper file could not be found:\n{WallpaperPath}", "Open Wallpaper");
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
            _dialogService.ShowError($"Failed to open the wallpaper in the default viewer.\n\n{ex.Message}", "Open Wallpaper");
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



