using Microsoft.Extensions.Logging.Abstractions;
using WindowsThemeManager.Core.Services;

namespace WindowsThemeManager.Tests.Unit;

public class MonitorServiceTests
{
    private readonly MonitorService _monitorService;

    public MonitorServiceTests()
    {
        var logger = NullLogger<MonitorService>.Instance;
        _monitorService = new MonitorService(logger);
    }

    [Fact]
    public async Task GetMonitorLayoutAsync_ReturnsValidLayout()
    {
        // Act
        var layout = await _monitorService.GetMonitorLayoutAsync();

        // Assert
        Assert.NotNull(layout);
        Assert.NotEmpty(layout.Monitors);
        Assert.True(layout.Monitors.Count >= 1, "Should detect at least one monitor");

        foreach (var monitor in layout.Monitors)
        {
            Assert.False(string.IsNullOrEmpty(monitor.DeviceName));
            Assert.True(monitor.MonitorNumber >= 1);
            Assert.True(monitor.Bounds.Width > 0);
            Assert.True(monitor.Bounds.Height > 0);
        }

        // At least one monitor should be primary
        Assert.Contains(layout.Monitors, m => m.IsPrimary);
    }

}
