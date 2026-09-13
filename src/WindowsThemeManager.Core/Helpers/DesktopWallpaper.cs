using System.Runtime.InteropServices;

namespace WindowsThemeManager.Core.Helpers;

/// <summary>
/// COM interop for the IDesktopWallpaper interface (Windows 8+).
/// Used to get/set per-monitor wallpapers.
/// CLSID: {C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD}
/// IID:   {B92B56A9-8B55-4E14-9A89-0199BBB6F93B}
/// </summary>
[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaper
{
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
                      [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    int GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
                     [MarshalAs(UnmanagedType.LPWStr)] out string? wallpaper);

    int GetMonitorDevicePathAt(uint monitorIndex,
                               [MarshalAs(UnmanagedType.LPWStr)] out string monitorID);

    int GetMonitorDevicePathCount(out uint count);

    int GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                       [MarshalAs(UnmanagedType.Struct)] ref RECT displayRect);

    void SetBackgroundColor(uint color);

    uint GetBackgroundColor();

    void SetPosition(DT_WALLPAPER_POSITION position);

    DT_WALLPAPER_POSITION GetPosition();

    void SetSlideshow(IntPtr items); // IShellItemArray

    void GetSlideshow(out Guid items);

    void SetSlideshowOptions(DT_SLIDESHOW_OPTIONS options, uint slideshowTick);

    void GetSlideshowOptions(out DT_SLIDESHOW_OPTIONS options, out uint slideshowTick);

    void AdvanceSlideshow(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
        DT_SLIDESHOW_DIRECTION direction);

    DT_SLIDESHOW_STATE GetStatus();

    void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

/// <summary>
/// COM class for creating IDesktopWallpaper instances.
/// </summary>
[ComImport]
[Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
public class DesktopWallpaperClass { }

/// <summary>
/// Wallpaper position enum.
/// </summary>
public enum DT_WALLPAPER_POSITION : uint
{
    DWPOS_CENTER = 0,
    DWPOS_TILE,
    DWPOS_STRETCH,
    DWPOS_FIT,
    DWPOS_FILL,
    DWPOS_SPAN
}

/// <summary>
/// Slideshow options flags.
/// </summary>
[Flags]
public enum DT_SLIDESHOW_OPTIONS : uint
{
    DWSO_NONE = 0x00000000,
    DWSO_SHUFFLEIMAGES = 0x00000001,
}

/// <summary>
/// Slideshow direction enum.
/// </summary>
public enum DT_SLIDESHOW_DIRECTION : uint
{
    DSD_FORWARD = 0,
    DSD_BACKWARD = 1,
}

/// <summary>
/// Slideshow state flags.
/// </summary>
[Flags]
public enum DT_SLIDESHOW_STATE : uint
{
    DSS_ENABLED = 0x00000001,
    DSS_SLIDESHOW = 0x00000002,
    DSS_DISABLED_BY_REMOTE_SESSION = 0x00000004,
}

/// <summary>
/// RECT structure for COM interop.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}
