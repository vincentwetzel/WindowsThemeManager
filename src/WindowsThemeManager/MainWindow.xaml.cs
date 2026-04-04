using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsThemeManager.ViewModels;

namespace WindowsThemeManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] No MainViewModel");
            return;
        }

        if (mainVm.Monitors.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] No monitors loaded");
            return;
        }

        // Find the Canvas in the ItemsControl
        var canvas = FindVisualChild<Canvas>(MonitorsItemsControl);
        if (canvas == null)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Canvas not found");
            return;
        }

        var clickPoint = e.GetPosition(canvas);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Click at ({clickPoint.X}, {clickPoint.Y}) relative to Canvas");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Canvas.ActualWidth={canvas.ActualWidth} ActualHeight={canvas.ActualHeight}");

        foreach (var monitor in mainVm.Monitors)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Monitor {monitor.MonitorNumber}: Canvas=({monitor.CanvasLeft},{monitor.CanvasTop},{monitor.CanvasWidth},{monitor.CanvasHeight})");

            double left = monitor.CanvasLeft;
            double top = monitor.CanvasTop;
            double right = left + monitor.CanvasWidth;
            double bottom = top + monitor.CanvasHeight;

            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Bounds: X=[{left},{right}] Y=[{top},{bottom}]");

            if (clickPoint.X >= left && clickPoint.X <= right &&
                clickPoint.Y >= top && clickPoint.Y <= bottom)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] *** CLICK INSIDE monitor {monitor.MonitorNumber}: {monitor.DeviceName}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] WallpaperPath: {monitor.WallpaperPath ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] File exists: {monitor.WallpaperPath != null && System.IO.File.Exists(monitor.WallpaperPath)}");

                if (!string.IsNullOrEmpty(monitor.WallpaperPath) && System.IO.File.Exists(monitor.WallpaperPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Opening in Photos: {monitor.WallpaperPath}");
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = monitor.WallpaperPath,
                            UseShellExecute = true
                        };
                        var proc = System.Diagnostics.Process.Start(psi);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Process started: {proc?.Id ?? 0}");
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] No valid wallpaper to open");
                }

                e.Handled = true;
                return;
            }
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG] Click did not hit any monitor");
    }

    private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
