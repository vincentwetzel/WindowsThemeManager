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

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string? GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    uint GetMonitorDevicePathCount();

    RECT GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

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
/// COM interface for IConnectionPointContainer.
/// CLSID: {C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD}
/// IID: {B196B284-BAB4-101A-B69C-00AA00341D07} (Standard COM IConnectionPointContainer)
/// </summary>
[ComImport]
[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IConnectionPointContainer
{
    void EnumConnectionPoints(out IEnumConnectionPoints ppEnum);

    [PreserveSig]
    int FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP);
}

/// <summary>
/// COM interface for enumerating connection points.
/// IID: {B196B285-BAB4-101A-B69C-00AA00341D07}
/// </summary>
[ComImport]
[Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumConnectionPoints
{
    [PreserveSig]
    int Next(
        int cConnections,
        [MarshalAs(UnmanagedType.Interface)] out IConnectionPoint ppCP,
        out int pcFetched);

    [PreserveSig]
    int Skip(int cConnections);

    [PreserveSig]
    int Reset();

    void Clone(out IEnumConnectionPoints ppEnum);
}

/// <summary>
/// COM interface for IConnectionPoint.
/// CLSID: {C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD}
/// IID: {B196B286-BAB4-101A-B69C-00AA00341D07} (Standard COM IConnectionPoint)
/// </summary>
[ComImport]
[Guid("B196B286-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IConnectionPoint
{
    void GetConnectionInterface(out Guid pIID);
    void GetConnectionPointContainer(out IConnectionPointContainer ppCPC);

    [PreserveSig]
    int Advise(
        [MarshalAs(UnmanagedType.IUnknown)] object pUnkSink,
        out int pdwCookie);

    [PreserveSig]
    int Unadvise(int dwCookie);
    void EnumConnections(out System.Runtime.InteropServices.ComTypes.IEnumConnections ppEnum);
}

/// <summary>
/// Callback interface for receiving desktop wallpaper change notifications.
/// IID: {BB21B7CD-86F8-4384-B88A-4FE7BF1D4C87}
/// </summary>
[ComVisible(true)]
[Guid("BB21B7CD-86F8-4384-B88A-4FE7BF1D4C87")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaperAdviseCallback
{
    [PreserveSig]
    int OnWallpaperChanged(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
        [MarshalAs(UnmanagedType.LPWStr)] string? wallpaper);
}

/// <summary>
/// COM class for creating IDesktopWallpaper instances.
/// </summary>
[ComImport]
[Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
public class DesktopWallpaperClass { }

/// <summary>
/// Win32 event hooks for detecting wallpaper changes.
/// Uses SetWinEventHook to receive system events when the desktop changes.
/// </summary>
public static class WallpaperChangeEvent
{
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    public const uint EVENT_OBJECT_CREATE = 0x8000;
    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    
    /// <summary>
    /// Delegate for WinEventProc callback.
    /// </summary>
    public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    /// <summary>
    /// Sets a hook to detect wallpaper changes via system events.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    /// <summary>
    /// Removes a previously installed event hook.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
}

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
