using System.Runtime.InteropServices;

namespace WindowsThemeManager.Core.Helpers;

/// <summary>
/// Win32 interop for applying wallpapers and visual styles.
/// </summary>
public static class ThemeApplierNative
{
    // SystemParametersInfo constants
    public const uint SPI_SETDESKWALLPAPER = 0x0014;
    public const uint SPIF_UPDATEINIFILE = 0x01;
    public const uint SPIF_SENDCHANGE = 0x02;

    /// <summary>
    /// Sets the desktop wallpaper for all monitors.
    /// </summary>
    /// <param name="wallpaperPath">Full path to the image file, or null to remove wallpaper.</param>
    /// <returns>True if successful.</returns>
    public static bool SetWallpaper(string? wallpaperPath)
    {
        var result = SystemParametersInfo(
            SPI_SETDESKWALLPAPER,
            0,
            wallpaperPath,
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

        return result;
    }

    /// <summary>
    /// Applies a visual style (.msstyles) to the system.
    /// </summary>
    /// <param name="stylePath">Full path to the .msstyles file.</param>
    /// <returns>HRESULT value (0 = success).</returns>
    public static int ApplyVisualStyle(string stylePath)
    {
        // uxtheme.dll!SetVisualStyles is not publicly documented.
        // The supported approach is to apply a .theme file which includes visual styles.
        // We use rundll32 as a workaround, or apply the entire .theme file.
        return ApplyThemeFileViaRundll(stylePath);
    }

    /// <summary>
    /// Applies a theme by launching the Windows theme control panel utility.
    /// This is the most reliable way to apply a complete .theme file.
    /// </summary>
    /// <param name="themePath">Full path to the .theme file.</param>
    public static void ApplyThemeByPath(string themePath)
    {
        // The canonical way to apply a .theme file programmatically
        ShellExecute(
            IntPtr.Zero,
            "open",
            themePath,
            null,
            null,
            SW_SHOWNORMAL);
    }

    /// <summary>
    /// Broadcasts a settings change to all top-level windows.
    /// Call this after changing wallpaper or other display settings.
    /// </summary>
    public static void BroadcastSettingsChange()
    {
        SendMessageTimeout(
            HWND_BROADCAST,
            WM_SETTINGCHANGE,
            IntPtr.Zero,
            IntPtr.Zero,
            SMTO_ABORTIFHUNG,
            5000,
            out _);
    }

    /// <summary>
    /// Applies a theme file using rundll32 (alternate method).
    /// </summary>
    private static int ApplyThemeFileViaRundll(string themePath)
    {
        var args = $"shell32.dll,Control_RunDLL desk.cpl desk,@Themes /Action:OpenTheme /File:\"{themePath}\"";
        var result = ShellExecute(
            IntPtr.Zero,
            "open",
            "rundll32.exe",
            args,
            null,
            SW_SHOWNORMAL);

        return result.ToInt32();
    }

    #region P/Invoke Declarations

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        string? pvParam,
        uint fWinIni);

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr ShellExecute(
        IntPtr hwnd,
        string lpOperation,
        string lpFile,
        string? lpParameters,
        string? lpDirectory,
        int nShowCmd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    private const int SW_SHOWNORMAL = 1;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

    #endregion
}
