using System.Runtime.InteropServices;

namespace WindowsThemeManager.Core.Helpers;

/// <summary>
/// Win32 interop for applying Windows themes.
/// </summary>
public static class ThemeApplierNative
{
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

    #region P/Invoke Declarations

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
