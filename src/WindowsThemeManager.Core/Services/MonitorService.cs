using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WindowsThemeManager.Core.Extensions;
using WindowsThemeManager.Core.Helpers;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Detects connected monitors and their configuration using Win32 APIs.
/// Uses IDesktopWallpaper COM interface for per-monitor wallpaper detection.
/// </summary>
public class MonitorService : Interfaces.IMonitorService
{
    private static readonly TimeSpan WallpaperPollInterval = TimeSpan.FromSeconds(2);
    private readonly ILogger<MonitorService> _logger;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private readonly Dictionary<string, string?> _cachedWallpapers = new();

#pragma warning disable CS0067 // Event is reserved for future use
    public event EventHandler? MonitorConfigurationChanged;
#pragma warning restore CS0067

    public event EventHandler<(string DevicePath, string? WallpaperPath)>? WallpaperChanged;

    public MonitorService(ILogger<MonitorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<MonitorLayout> GetMonitorLayoutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var layout = new MonitorLayout();
        var monitors = new List<MonitorInfo>();

        // Use IDesktopWallpaper COM interface to get per-monitor wallpapers correctly
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            uint monitorCount = desktopWallpaper.GetMonitorDevicePathCount();

            _logger.LogInformation("IDesktopWallpaper reports {Count} monitors", monitorCount);
            Console.WriteLine($"[MonitorService] IDesktopWallpaper reports {monitorCount} monitors");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] IDesktopWallpaper reports {monitorCount} monitors");

            for (uint i = 0; i < monitorCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string devicePath = desktopWallpaper.GetMonitorDevicePathAt(i);
                string? wallpaperPath = desktopWallpaper.GetWallpaper(devicePath);

                // Get monitor bounds from COM interface
                RECT rect = desktopWallpaper.GetMonitorRECT(devicePath);
                var bounds = new WindowsThemeManager.Core.Models.IntRect(
                    rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

                var monitor = new MonitorInfo
                {
                    DeviceName = devicePath,
                    MonitorNumber = (int)i + 1,
                    Bounds = bounds,
                    WorkingArea = bounds,
                    IsPrimary = (i == 0), // First monitor is typically primary
                    CurrentWallpaperPath = string.IsNullOrEmpty(wallpaperPath) ? null : wallpaperPath,
                };

                monitors.Add(monitor);
                _logger.LogDebug("Found monitor {Num}: {DeviceName} ({Width}x{Height}), Wallpaper: {Wallpaper}",
                    monitor.MonitorNumber, devicePath, bounds.Width, bounds.Height,
                    string.IsNullOrEmpty(wallpaperPath) ? "(none)" : wallpaperPath);
                Console.WriteLine($"[MonitorService] Found monitor {monitor.MonitorNumber}: {devicePath} ({bounds.Width}x{bounds.Height}), Wallpaper: {(string.IsNullOrEmpty(wallpaperPath) ? "(none)" : wallpaperPath)}");
                System.Diagnostics.Debug.WriteLine($"[MonitorService] Found monitor {monitor.MonitorNumber}: {devicePath} ({bounds.Width}x{bounds.Height}), Wallpaper: {(string.IsNullOrEmpty(wallpaperPath) ? "(none)" : wallpaperPath)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get monitors via IDesktopWallpaper, falling back to Screen.AllScreens");

            // Fallback to Screen.AllScreens - uses system-wide wallpaper for all monitors
            var allMonitors = System.Windows.Forms.Screen.AllScreens;
            var systemWallpaper = GetCurrentWallpaperPath();
            int monitorIndex = 0;

            foreach (var screen in allMonitors)
            {
                monitorIndex++;

                var monitor = new MonitorInfo
                {
                    DeviceName = screen.DeviceName,
                    MonitorNumber = monitorIndex,
                    Bounds = screen.Bounds.ToIntRect(),
                    WorkingArea = screen.WorkingArea.ToIntRect(),
                    IsPrimary = screen.Primary,
                    CurrentWallpaperPath = systemWallpaper, // Use system-wide wallpaper as fallback
                };

                monitors.Add(monitor);
                _logger.LogDebug("Found monitor (fallback): {DeviceName} ({Width}x{Height}) Primary={Primary}",
                    screen.DeviceName, screen.Bounds.Width, screen.Bounds.Height, screen.Primary);
                Console.WriteLine($"[MonitorService] Found monitor (fallback): {screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height}) Primary={screen.Primary}");
                System.Diagnostics.Debug.WriteLine($"[MonitorService] Found monitor (fallback): {screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height}) Primary={screen.Primary}");
            }
        }

        layout.Monitors = monitors;

        // Calculate total bounds
        if (monitors.Count > 0)
        {
            layout.TotalBounds = IntRect.Union(monitors.Select(m => m.Bounds).ToArray());
        }

        _logger.LogInformation("Detected {Count} monitors, total bounds: {TotalBounds}",
            monitors.Count, layout.TotalBounds);
        Console.WriteLine($"[MonitorService] Detected {monitors.Count} monitors, total bounds: {layout.TotalBounds}");
        System.Diagnostics.Debug.WriteLine($"[MonitorService] Detected {monitors.Count} monitors, total bounds: {layout.TotalBounds}");

        return Task.FromResult(layout);
    }

    /// <inheritdoc />
    public async Task<string?> GetMonitorWallpaperAsync(int monitorIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            var devicePath = desktopWallpaper.GetMonitorDevicePathAt((uint)monitorIndex);
            var wallpaper = desktopWallpaper.GetWallpaper(devicePath);
            return string.IsNullOrEmpty(wallpaper) ? GetCurrentWallpaperPath() : wallpaper;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get monitor wallpaper at index {Index}", monitorIndex);
            Console.WriteLine($"[MonitorService] Failed to get monitor wallpaper at index {monitorIndex}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Failed to get monitor wallpaper at index {monitorIndex}: {ex.Message}");
            return GetCurrentWallpaperPath();
        }
    }

    /// <summary>
    /// Gets the system-wide current wallpaper from registry.
    /// </summary>
    private static string? GetCurrentWallpaperPath()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            return key?.GetValue("WallPaper") as string;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Starts polling for wallpaper change events.
    /// Polls IDesktopWallpaper.GetWallpaper() every 5 seconds for each monitor.
    /// IDesktopWallpaper is the definitive source of truth for per-monitor wallpaper state.
    /// This is the only polling mechanism in the codebase.
    /// </summary>
    public void StartListeningForWallpaperChanges()
    {
        if (_pollingCts != null)
        {
            Console.WriteLine("[MonitorService] Already polling for wallpaper changes, skipping");
            System.Diagnostics.Debug.WriteLine("[MonitorService] Already polling, skipping");
            return;
        }

        try
        {
            Console.WriteLine("[MonitorService] Starting wallpaper change detection via IDesktopWallpaper polling (5s interval)");
            System.Diagnostics.Debug.WriteLine("[MonitorService] Starting wallpaper change detection via IDesktopWallpaper polling (5s interval)");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] StartListeningForWallpaperChanges - polling IDesktopWallpaper every 5s");

            _pollingCts = new CancellationTokenSource();
            _pollingTask = Task.Run(() => PollWallpaperChangesAsync(_pollingCts.Token), _pollingCts.Token);

            _logger.LogInformation("Started polling for wallpaper change events via IDesktopWallpaper (every 5s)");
            Console.WriteLine("[MonitorService] Started polling for wallpaper change events via IDesktopWallpaper");
            System.Diagnostics.Debug.WriteLine("[MonitorService] Started polling for wallpaper change events via IDesktopWallpaper");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling task started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start wallpaper polling");
            Console.WriteLine($"[MonitorService] Failed to start wallpaper polling: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Failed to start wallpaper polling: {ex.Message}");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling startup failed: {ex}");
            _pollingCts = null;
            _pollingTask = null;
        }
    }

    /// <summary>
    /// Polls IDesktopWallpaper.GetWallpaper() for each monitor and fires events when wallpapers change.
    /// </summary>
    private async Task PollWallpaperChangesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Wallpaper polling loop started");
        Console.WriteLine("[MonitorService] Wallpaper polling loop started");
        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper polling loop started");

        // Initialize cache with current state
        await InitializeWallpaperCacheAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(WallpaperPollInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await CheckForWallpaperChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during wallpaper polling cycle");
                Console.WriteLine($"[MonitorService] Error during wallpaper polling cycle: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MonitorService] Error during wallpaper polling cycle: {ex.Message}");
                Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling cycle error: {ex.Message}");
            }
        }

        _logger.LogDebug("Wallpaper polling loop stopped");
        Console.WriteLine("[MonitorService] Wallpaper polling loop stopped");
        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper polling loop stopped");
    }

    /// <summary>
    /// Initializes the cached wallpaper state with current per-monitor wallpapers.
    /// </summary>
    private async Task InitializeWallpaperCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            uint monitorCount = desktopWallpaper.GetMonitorDevicePathCount();

            lock (_cachedWallpapers)
            {
                _cachedWallpapers.Clear();

                for (uint i = 0; i < monitorCount; i++)
                {
                    string devicePath = desktopWallpaper.GetMonitorDevicePathAt(i);
                    string? wallpaperPath = desktopWallpaper.GetWallpaper(devicePath);
                    _cachedWallpapers[devicePath] = string.IsNullOrEmpty(wallpaperPath) ? null : wallpaperPath;
                }
            }

            _logger.LogDebug("Initialized wallpaper cache with {Count} monitors", monitorCount);
            Console.WriteLine($"[MonitorService] Initialized wallpaper cache with {monitorCount} monitors");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Initialized wallpaper cache with {monitorCount} monitors");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper cache initialized: {monitorCount} monitors");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize wallpaper cache");
            Console.WriteLine($"[MonitorService] Failed to initialize wallpaper cache: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Failed to initialize wallpaper cache: {ex.Message}");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Cache init failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks for wallpaper changes by comparing current state with cached state.
    /// Fires WallpaperChanged event for any monitor whose wallpaper has changed.
    /// </summary>
    private async Task CheckForWallpaperChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            uint monitorCount = desktopWallpaper.GetMonitorDevicePathCount();

            List<string> changedMonitors = new();

            lock (_cachedWallpapers)
            {
                for (uint i = 0; i < monitorCount; i++)
                {
                    string devicePath = desktopWallpaper.GetMonitorDevicePathAt(i);
                    string? currentWallpaper = desktopWallpaper.GetWallpaper(devicePath);
                    currentWallpaper = string.IsNullOrEmpty(currentWallpaper) ? null : currentWallpaper;

                    if (_cachedWallpapers.TryGetValue(devicePath, out string? cachedWallpaper))
                    {
                        bool changed = !string.Equals(currentWallpaper, cachedWallpaper, StringComparison.OrdinalIgnoreCase);
                        if (changed)
                        {
                            changedMonitors.Add(devicePath);
                            Console.WriteLine($"[MonitorService] Polling detected wallpaper change on monitor {i + 1} ({devicePath})");
                            Console.WriteLine($"[MonitorService]   Old: {cachedWallpaper ?? "(none)"}");
                            Console.WriteLine($"[MonitorService]   New: {currentWallpaper ?? "(none)"}");
                            System.Diagnostics.Debug.WriteLine($"[MonitorService] Wallpaper changed on monitor {i + 1}: {devicePath}");
                            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper change detected on monitor {i + 1} ({devicePath})");

                            // Update cache
                            _cachedWallpapers[devicePath] = currentWallpaper;
                        }
                    }
                    else
                    {
                        // New monitor detected
                        _cachedWallpapers[devicePath] = currentWallpaper;
                        changedMonitors.Add(devicePath);
                        Console.WriteLine($"[MonitorService] Polling detected new monitor: {devicePath}");
                        System.Diagnostics.Debug.WriteLine($"[MonitorService] New monitor detected: {devicePath}");
                        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] New monitor detected: {devicePath}");
                    }
                }

                // Check for removed monitors
                var removedDevices = _cachedWallpapers.Keys
                    .Where(k => !Enumerable.Range(0, (int)monitorCount)
                        .Select(i => desktopWallpaper.GetMonitorDevicePathAt((uint)i))
                        .Contains(k))
                    .ToList();

                foreach (var removedDevice in removedDevices)
                {
                    _cachedWallpapers.Remove(removedDevice);
                    Console.WriteLine($"[MonitorService] Monitor removed: {removedDevice}");
                    System.Diagnostics.Debug.WriteLine($"[MonitorService] Monitor removed: {removedDevice}");
                    Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Monitor removed: {removedDevice}");
                }
            }

            // Fire events for all changed monitors
            foreach (var devicePath in changedMonitors)
            {
                string? newWallpaper = null;
                lock (_cachedWallpapers)
                {
                    _cachedWallpapers.TryGetValue(devicePath, out newWallpaper);
                }

                Console.WriteLine($"[MonitorService] Firing WallpaperChanged for {devicePath}: {newWallpaper ?? "(none)"}");
                System.Diagnostics.Debug.WriteLine($"[MonitorService] Firing WallpaperChanged for {devicePath}");
                Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Firing WallpaperChanged for {devicePath}");

                WallpaperChanged?.Invoke(this, (devicePath, newWallpaper));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for wallpaper changes");
            Console.WriteLine($"[MonitorService] Failed to check for wallpaper changes: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Failed to check for wallpaper changes: {ex.Message}");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops polling for wallpaper change events.
    /// </summary>
    public void StopListeningForWallpaperChanges()
    {
        if (_pollingCts == null)
            return;

        try
        {
            Console.WriteLine("[MonitorService] Stopping wallpaper polling");
            System.Diagnostics.Debug.WriteLine("[MonitorService] Stopping wallpaper polling");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] StopListeningForWallpaperChanges called");

            _pollingCts.Cancel();
            _pollingTask?.Wait(TimeSpan.FromSeconds(2));
            _pollingCts.Dispose();
            _pollingCts = null;
            _pollingTask = null;

            lock (_cachedWallpapers)
            {
                _cachedWallpapers.Clear();
            }

            _logger.LogInformation("Stopped wallpaper polling");
            Console.WriteLine("[MonitorService] Stopped wallpaper polling");
            System.Diagnostics.Debug.WriteLine("[MonitorService] Stopped wallpaper polling");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop wallpaper polling");
            Console.WriteLine($"[MonitorService] Failed to stop wallpaper polling: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Failed to stop wallpaper polling: {ex.Message}");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling stop error: {ex}");
        }
    }

    #region Win32 API Declarations (reserved for future use)

    // [DllImport("user32.dll")]
    // private static extern bool EnumDisplayMonitors(
    //     IntPtr hdc,
    //     IntPtr lprcClip,
    //     MonitorEnumProc lpfnEnum,
    //     IntPtr dwData);

    // private delegate bool MonitorEnumProc(
    //     IntPtr hMonitor,
    //     IntPtr hdc,
    //     IntPtr lprcMonitor,
    //     IntPtr dwData);

    // [DllImport("user32.dll")]
    // private static extern bool GetMonitorInfo(
    //     IntPtr hMonitor,
    //     ref MONITORINFO lpmi);

    // [StructLayout(LayoutKind.Sequential)]
    // private struct MONITORINFO
    // {
    //     public uint cbSize;
    //     public RECT rcMonitor;
    //     public RECT rcWork;
    //     public uint dwFlags;
    // }

    // [StructLayout(LayoutKind.Sequential)]
    // private struct RECT
    // {
    //     public int left;
    //     public int top;
    //     public int right;
    //     public int bottom;
    // }

    #endregion
}
