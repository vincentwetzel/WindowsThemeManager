using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using WindowsThemeManager.Core.Models;
using WindowsThemeManager.Services;
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
        if (string.IsNullOrEmpty(WallpaperPath))
        {
            _dialogService.ShowWarning($"No wallpaper file is associated with monitor {MonitorNumber}.", "Open Wallpaper");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
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
            return;
        }

        try
        {
            // Move file to recycle bin using SHFileOperation
            bool success = MoveToRecycleBin(WallpaperPath);
            
            if (success)
            {
                WallpaperPath = null;
                WallpaperPreview = null;
                _dialogService.ShowInfo(
                    $"Wallpaper has been moved to the Recycle Bin.\n\nFile: {fileName}",
                    "Wallpaper Deleted");
            }
            else
            {
                _dialogService.ShowError(
                    $"Failed to move the wallpaper to the Recycle Bin.\n\nFile: {fileName}",
                    "Delete Error");
            }
        }
        catch (Exception ex)
        {
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
        catch (Exception)
        {
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
        if (string.IsNullOrEmpty(WallpaperPath))
        {
            _dialogService.ShowWarning($"No wallpaper file is associated with monitor {MonitorNumber}.", "Open Wallpaper");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
            _dialogService.ShowError($"The wallpaper file could not be found:\n{WallpaperPath}", "Open Wallpaper");
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = WallpaperPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to open the wallpaper in the default viewer.\n\n{ex.Message}", "Open Wallpaper");
        }
    }

    /// <summary>
    /// Loads the wallpaper thumbnail for display.
    /// </summary>
    public async Task LoadWallpaperPreviewAsync()
    {
        if (string.IsNullOrEmpty(WallpaperPath))
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} has no wallpaper path");
            return;
        }

        if (!File.Exists(WallpaperPath))
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} wallpaper file missing path={WallpaperPath}");
            return;
        }

        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Loading monitor {MonitorNumber} wallpaper path={WallpaperPath}");
        // Clear the previous image first so the UI always sees a concrete transition.
        WallpaperPreview = null;
        WallpaperPreview = await _imageService.LoadThumbnailAsync(WallpaperPath);
        
        if (WallpaperPreview == null)
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorItemViewModel] Monitor {MonitorNumber} thumbnail load returned null");
        }
        else
        {
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



