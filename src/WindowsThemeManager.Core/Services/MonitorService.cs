using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
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
            _ = desktopWallpaper.GetMonitorDevicePathCount(out uint monitorCount);

            _logger.LogInformation("IDesktopWallpaper reports {Count} monitors", monitorCount);
            Console.WriteLine($"[MonitorService] IDesktopWallpaper reports {monitorCount} monitors");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] IDesktopWallpaper reports {monitorCount} monitors");

            // IDesktopWallpaper reliably supplies the monitor identity and its
            // wallpaper, but GetMonitorRECT returns E_FAIL on some Windows builds.
            // Screen.AllScreens provides the same virtual-screen geometry without
            // losing the per-monitor wallpaper paths.
            var screenMonitors = System.Windows.Forms.Screen.AllScreens;
            if (screenMonitors.Length != monitorCount)
            {
                _logger.LogWarning(
                    "Monitor count mismatch: wallpaper API={WallpaperCount}, screen API={ScreenCount}; using visible screen count",
                    monitorCount, screenMonitors.Length);
            }

            // The wallpaper API can include an extra shell/virtual entry. Only
            // create tiles for visible screens, while retaining the per-monitor
            // wallpaper identity from the matching API entry.
            int visibleMonitorCount = Math.Min((int)monitorCount, screenMonitors.Length);
            var settingsOrderedScreens = GetSettingsOrderedScreens(screenMonitors);
            for (int i = 0; i < visibleMonitorCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _ = desktopWallpaper.GetMonitorDevicePathAt((uint)i, out string devicePath);
                _ = desktopWallpaper.GetWallpaper(devicePath, out string? wallpaperPath);

                // Keep the stable pairing between the wallpaper API's entries and
                // the visible screen enumeration. Do not invoke the experimental
                // DisplayConfig interop here; malformed native layouts can corrupt
                // the process heap.
                // IDesktopWallpaper enumerates wallpaper entries in Windows
                // Display Settings order: 1 (primary), 2 (left), 3 (right),
                // followed by the display above the primary in this layout.
                var screen = settingsOrderedScreens[i];
                var bounds = screen.Bounds.ToIntRect();

                var monitor = new MonitorInfo
                {
                    DeviceName = devicePath,
                    MonitorNumber = i + 1,
                    Bounds = bounds,
                    WorkingArea = screen.WorkingArea.ToIntRect(),
                    IsPrimary = screen.Primary,
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

            // The COM loop may have populated some monitors before a later query
            // failed. Discard those partial results before adding fallback screens.
            monitors.Clear();

            // Fallback to Screen.AllScreens for geometry only.  The registry value is
            // system-wide and is not safe to use as a per-monitor wallpaper path:
            // when Windows has different wallpapers (or a slideshow) it can point to
            // one shared/transcoded image and would make every preview identical.
            var allMonitors = System.Windows.Forms.Screen.AllScreens;
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
                    CurrentWallpaperPath = null,
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
            _ = desktopWallpaper.GetMonitorDevicePathAt((uint)monitorIndex, out string devicePath);
            _ = desktopWallpaper.GetWallpaper(devicePath, out string? wallpaper);
            // Do not fall back to Control Panel\Desktop\WallPaper here. That value
            // represents a shared desktop wallpaper and cannot identify this monitor.
            return string.IsNullOrEmpty(wallpaper) ? null : wallpaper;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get monitor wallpaper at index {Index}", monitorIndex);
            Console.WriteLine($"[MonitorService] Failed to get monitor wallpaper at index {monitorIndex}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MonitorService] Failed to get monitor wallpaper at index {monitorIndex}: {ex.Message}");
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
            _ = desktopWallpaper.GetMonitorDevicePathCount(out uint monitorCount);

            lock (_cachedWallpapers)
            {
                _cachedWallpapers.Clear();

                for (uint i = 0; i < monitorCount; i++)
                {
                    _ = desktopWallpaper.GetMonitorDevicePathAt(i, out string devicePath);
                    _ = desktopWallpaper.GetWallpaper(devicePath, out string? wallpaperPath);
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
            _ = desktopWallpaper.GetMonitorDevicePathCount(out uint monitorCount);

            List<string> changedMonitors = new();

            lock (_cachedWallpapers)
            {
                for (uint i = 0; i < monitorCount; i++)
                {
                    _ = desktopWallpaper.GetMonitorDevicePathAt(i, out string devicePath);
                    _ = desktopWallpaper.GetWallpaper(devicePath, out string? currentWallpaper);
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
                        .Select(i => GetMonitorDevicePath(desktopWallpaper, (uint)i))
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

    private static string GetMonitorDevicePath(IDesktopWallpaper desktopWallpaper, uint index)
    {
        _ = desktopWallpaper.GetMonitorDevicePathAt(index, out string devicePath);
        return devicePath;
    }

    /// <summary>
    /// Matches the shell monitor path to the physical screen. The COM path uses
    /// identifiers such as DISPLAY#ACI249A#... while EnumDisplayDevices exposes
    /// the same hardware identity as MONITOR\ACI249A\....
    /// </summary>
    private static DisplayConfigMonitor? FindScreenForMonitor(
        string monitorPath,
        System.Windows.Forms.Screen[] screens)
    {
        var displayMap = GetDisplayConfigMap(screens);
        return displayMap.TryGetValue(monitorPath, out var screen) ? screen : null;
    }

    private static string NormalizeMonitorId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return string.Empty;

        return deviceId.Trim();
    }

    private static int GetWindowsDisplayNumber(
        System.Windows.Forms.Screen screen,
        int fallbackNumber)
    {
        var name = screen.DeviceName;
        const string prefix = "DISPLAY";
        var marker = name.LastIndexOf(prefix, StringComparison.OrdinalIgnoreCase);

        return marker >= 0 && int.TryParse(name[(marker + prefix.Length)..], out var number)
            ? number
            : fallbackNumber;
    }

    private static System.Windows.Forms.Screen[] GetSettingsOrderedScreens(
        System.Windows.Forms.Screen[] screens)
    {
        var primary = screens.FirstOrDefault(s => s.Primary);
        if (primary == null)
            return screens;

        var remaining = screens.Where(s => !ReferenceEquals(s, primary)).ToList();
        var ordered = new List<System.Windows.Forms.Screen> { primary };

        AddFirst(remaining.Where(s => s.Bounds.Right <= primary.Bounds.Left)
            .OrderByDescending(s => s.Bounds.X));
        AddFirst(remaining.Where(s => s.Bounds.Left >= primary.Bounds.Right)
            .OrderBy(s => s.Bounds.X));
        AddFirst(remaining.Where(s => s.Bounds.Bottom <= primary.Bounds.Top)
            .OrderByDescending(s => s.Bounds.Y));

        ordered.AddRange(remaining.OrderBy(s => s.Bounds.Y).ThenBy(s => s.Bounds.X));
        return ordered.ToArray();

        void AddFirst(IEnumerable<System.Windows.Forms.Screen> candidates)
        {
            var screen = candidates.FirstOrDefault(s => remaining.Contains(s));
            if (screen != null)
            {
                ordered.Add(screen);
                remaining.Remove(screen);
            }
        }
    }

    private static Dictionary<string, DisplayConfigMonitor> GetDisplayConfigMap(
        System.Windows.Forms.Screen[] screens)
    {
        var result = new Dictionary<string, DisplayConfigMonitor>(StringComparer.OrdinalIgnoreCase);
        uint pathCount = 0;
        uint modeCount = 0;

        if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount) != 0)
            return result;

        var pathSize = Marshal.SizeOf<DISPLAYCONFIG_PATH_INFO>();
        var modeSize = Marshal.SizeOf<DISPLAYCONFIG_MODE_INFO>();
        var pathBuffer = Marshal.AllocHGlobal(checked((int)pathCount * pathSize));
        var modeBuffer = Marshal.AllocHGlobal(checked((int)modeCount * modeSize));

        try
        {
            var requestedPathCount = pathCount;
            var requestedModeCount = modeCount;
            if (QueryDisplayConfig(
                    QDC_ONLY_ACTIVE_PATHS,
                    ref requestedPathCount,
                    pathBuffer,
                    ref requestedModeCount,
                    modeBuffer,
                    IntPtr.Zero) != 0)
            {
                return result;
            }

            var displayNumber = 0;
            for (var i = 0; i < requestedPathCount; i++)
            {
                var path = Marshal.PtrToStructure<DISPLAYCONFIG_PATH_INFO>(
                    IntPtr.Add(pathBuffer, checked((int)i * pathSize)));

                var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = CreateDeviceInfoHeader(
                        DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        path.sourceInfo.adapterId,
                        path.sourceInfo.id)
                };
                var target = new DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = CreateDeviceInfoHeader(
                        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                        path.targetInfo.adapterId,
                        path.targetInfo.id)
                };

                if (DisplayConfigGetDeviceInfo(ref source) != 0 ||
                    DisplayConfigGetDeviceInfo(ref target) != 0)
                {
                    continue;
                }

                var screen = screens.FirstOrDefault(s =>
                    string.Equals(s.DeviceName, source.viewGdiDeviceName,
                        StringComparison.OrdinalIgnoreCase));
                if (screen != null && !string.IsNullOrWhiteSpace(target.monitorDevicePath))
                {
                    // QueryDisplayConfig's active-path order is the order used
                    // by Windows Display Settings for its monitor numbers.
                    displayNumber++;
                    result[target.monitorDevicePath] = new DisplayConfigMonitor(
                        screen, displayNumber);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pathBuffer);
            Marshal.FreeHGlobal(modeBuffer);
        }

        return result;
    }

    private sealed record DisplayConfigMonitor(
        System.Windows.Forms.Screen Screen,
        int Number);

    private static DISPLAYCONFIG_DEVICE_INFO_HEADER CreateDeviceInfoHeader(
        uint type,
        int size,
        LUID adapterId,
        uint id) => new()
        {
            type = type,
            size = (uint)size,
            adapterId = adapterId,
            id = id
        };

    private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
    private const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    private const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        IntPtr pathInfoArray,
        ref uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint lowPart;
        public int highPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL
    {
        public uint numerator;
        public uint denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        public int targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] modeInfo;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
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
