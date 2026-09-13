using System.Drawing;
using System.Diagnostics;
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
                // the visible screen enumeration.
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

        return Task.FromResult(layout);
    }

    /// <summary>
    /// Starts polling for wallpaper change events.
    /// Polls IDesktopWallpaper.GetWallpaper() every two seconds for each monitor.
    /// IDesktopWallpaper is the definitive source of truth for per-monitor wallpaper state.
    /// This is the only polling mechanism in the codebase.
    /// </summary>
    public void StartListeningForWallpaperChanges()
    {
        if (_pollingCts != null)
        {
            return;
        }

        try
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] StartListeningForWallpaperChanges - polling IDesktopWallpaper every {WallpaperPollInterval.TotalSeconds:0}s");

            _pollingCts = new CancellationTokenSource();
            _pollingTask = Task.Run(() => PollWallpaperChangesAsync(_pollingCts.Token), _pollingCts.Token);

            _logger.LogInformation("Started polling for wallpaper change events via IDesktopWallpaper (every {IntervalSeconds}s)", WallpaperPollInterval.TotalSeconds);
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling task started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start wallpaper polling");
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
        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper polling loop started");

        // Initialize cache with current state
        InitializeWallpaperCache();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(WallpaperPollInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                CheckForWallpaperChanges();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during wallpaper polling cycle");
                Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling cycle error: {ex.Message}");
            }
        }

        _logger.LogDebug("Wallpaper polling loop stopped");
        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper polling loop stopped");
    }

    /// <summary>
    /// Initializes the cached wallpaper state with current per-monitor wallpapers.
    /// </summary>
    private void InitializeWallpaperCache()
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
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper cache initialized: {monitorCount} monitors");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize wallpaper cache");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Cache init failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks for wallpaper changes by comparing current state with cached state.
    /// Fires WallpaperChanged event for any monitor whose wallpaper has changed.
    /// </summary>
    private void CheckForWallpaperChanges()
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            _ = desktopWallpaper.GetMonitorDevicePathCount(out uint monitorCount);

            var changedMonitors = new List<string>();
            var currentDevicePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (_cachedWallpapers)
            {
                for (uint i = 0; i < monitorCount; i++)
                {
                    _ = desktopWallpaper.GetMonitorDevicePathAt(i, out string devicePath);
                    _ = desktopWallpaper.GetWallpaper(devicePath, out string? currentWallpaper);
                    currentWallpaper = string.IsNullOrEmpty(currentWallpaper) ? null : currentWallpaper;
                    currentDevicePaths.Add(devicePath);

                    if (_cachedWallpapers.TryGetValue(devicePath, out string? cachedWallpaper))
                    {
                        bool changed = !string.Equals(currentWallpaper, cachedWallpaper, StringComparison.OrdinalIgnoreCase);
                        if (changed)
                        {
                            changedMonitors.Add(devicePath);
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
                        Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] New monitor detected: {devicePath}");
                    }
                }

                // Check for removed monitors
                var removedDevices = _cachedWallpapers.Keys
                    .Where(k => !currentDevicePaths.Contains(k))
                    .ToList();

                foreach (var removedDevice in removedDevices)
                {
                    _cachedWallpapers.Remove(removedDevice);
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

                Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Firing WallpaperChanged for {devicePath}");

                WallpaperChanged?.Invoke(this, (devicePath, newWallpaper));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for wallpaper changes");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Wallpaper check failed: {ex.Message}");
        }
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

    /// <summary>
    /// Stops polling for wallpaper change events.
    /// </summary>
    public void StopListeningForWallpaperChanges()
    {
        if (_pollingCts == null)
            return;

        try
        {
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
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop wallpaper polling");
            Trace.WriteLine($"[{DateTime.Now:O}] [MonitorService] Polling stop error: {ex}");
        }
    }

}
