using System.Windows;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Services;
using WindowsThemeManager.Services;
using WindowsThemeManager.ViewModels;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager;

/// <summary>
/// Application entry point with DI bootstrapping.
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private TextWriterTraceListener? _traceListener;

    /// <summary>
    /// Gets the application's service provider.
    /// </summary>
    public static IServiceProvider? Services => Current is App app ? app._serviceProvider : null;

    /// <summary>
    /// Configures the DI container and starts the application.
    /// </summary>
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            await RunStartup();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [App] FATAL ERROR during startup: {ex.GetType().Name}: {ex.Message}");
            Trace.WriteLine($"[{DateTime.Now:O}] [App] Stack trace: {ex.StackTrace}");
            System.Windows.MessageBox.Show(
                $"Fatal error during startup:\n\n{ex.GetType().Name}: {ex.Message}\n\nSee log for details.",
                "Windows Theme Manager - Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RunStartup()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsThemeManager",
            "Logs");
        Directory.CreateDirectory(logDirectory);

        // Clean up old log files (keep last 10)
        CleanupOldLogs(logDirectory, maxLogsToKeep: 10);

        // Create a new log file with timestamp for each run
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFileName = $"debug_{timestamp}.log";
        var logFilePath = Path.Combine(logDirectory, logFileName);

        _traceListener = new TextWriterTraceListener(logFilePath);
        Trace.Listeners.Add(_traceListener);
        Trace.AutoFlush = true;
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Startup");
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Debug log file: {logFilePath}");
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Process ID: {Environment.ProcessId}");

        Trace.WriteLine($"[{DateTime.Now:O}] [App] Building DI container...");
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Core services
        services.AddSingleton<IThemeDirectoryScanner, ThemeDirectoryScanner>();
        services.AddSingleton<IThemeFileParser, ThemeFileParser>();
        services.AddSingleton<IThemeApplier, ThemeApplier>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IDesktopIconService, DesktopIconService>();

        // Settings (load on startup)
        services.AddSingleton<SettingsService>();

        // UI services
        services.AddSingleton<IWallpaperImageService, WallpaperImageService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DesktopIconViewModel>();

        // Build the container
        _serviceProvider = services.BuildServiceProvider();
        Trace.WriteLine($"[{DateTime.Now:O}] [App] DI container built.");

        // Load settings synchronously to avoid threading issues
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Loading settings...");
        var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
        await settingsService.LoadAsync();
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Settings loaded. ThemeMode: {settingsService.Settings.ThemeMode}");

        // Create main window
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Creating MainWindow...");
        var mainWindow = new MainWindow();
        Trace.WriteLine($"[{DateTime.Now:O}] [App] MainWindow created.");

        Trace.WriteLine($"[{DateTime.Now:O}] [App] Resolving MainViewModel...");
        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        Trace.WriteLine($"[{DateTime.Now:O}] [App] MainViewModel resolved.");
        mainWindow.DataContext = viewModel;
        Trace.WriteLine($"[{DateTime.Now:O}] [App] DataContext set.");

        // Set initial theme
        try
        {
            mainWindow.SetThemeMode(settingsService.Settings.ThemeMode);
            Trace.WriteLine($"[{DateTime.Now:O}] [App] Theme mode set.");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [App] Error setting theme mode: {ex.Message}");
        }

        // Apply saved window settings
        if (settingsService.Settings.WindowMaximized)
            mainWindow.WindowState = System.Windows.WindowState.Maximized;
        else
        {
            mainWindow.Width = settingsService.Settings.WindowWidth;
            mainWindow.Height = settingsService.Settings.WindowHeight;
        }

        // Show the window
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Showing main window...");
        mainWindow.Show();
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Window shown. WindowState: {mainWindow.WindowState}, Width: {mainWindow.Width}, Height: {mainWindow.Height}");

        // Trigger initial load
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Triggering VM load...");
        _ = viewModel.LoadAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Trace.WriteLine($"[{DateTime.Now:O}] [App] Exit");

        // Stop listening for wallpaper changes
        if (_serviceProvider?.GetService<IMonitorService>() is MonitorService monitorService)
        {
            monitorService.StopListeningForWallpaperChanges();
        }

        // Cleanup
        if (_serviceProvider is IDisposable d)
            d.Dispose();

        if (_traceListener != null)
        {
            Trace.Flush();
            Trace.Listeners.Remove(_traceListener);
            _traceListener.Close();
            _traceListener.Dispose();
            _traceListener = null;
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Cleans up old log files, keeping only the most recent ones.
    /// </summary>
    private static void CleanupOldLogs(string logDirectory, int maxLogsToKeep)
    {
        try
        {
            var logFiles = Directory.GetFiles(logDirectory, "debug_*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (logFiles.Count > maxLogsToKeep)
            {
                var filesToDelete = logFiles.Skip(maxLogsToKeep).ToList();
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        Trace.WriteLine($"[{DateTime.Now:O}] [App] Deleted old log file: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[{DateTime.Now:O}] [App] Failed to delete old log file {file.Name}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.Now:O}] [App] Error cleaning up old logs: {ex.Message}");
        }
    }
}
