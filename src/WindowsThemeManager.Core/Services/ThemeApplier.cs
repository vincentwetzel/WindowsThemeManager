using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Helpers;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Services;

/// <summary>
/// Applies themes using Windows APIs.
/// </summary>
public class ThemeApplier : IThemeApplier
{
    private readonly ILogger<ThemeApplier> _logger;

    public ThemeApplier(ILogger<ThemeApplier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ApplyThemeAsync(Theme theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);

        _logger.LogInformation("Applying theme: {DisplayName}", theme.DisplayName);

        try
        {
            if (!File.Exists(theme.ThemePath))
            {
                _logger.LogWarning("Theme file not found: {ThemePath}", theme.ThemePath);
                return false;
            }

            ThemeApplierNative.ApplyThemeByPath(theme.ThemePath);
            _logger.LogInformation("Theme file applied: {ThemePath}", theme.ThemePath);

            // Broadcast the settings change
            ThemeApplierNative.BroadcastSettingsChange();

            // Small delay for the system to process
            await Task.Delay(500, cancellationToken);

            _logger.LogInformation("Theme applied successfully: {DisplayName}", theme.DisplayName);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme: {DisplayName}", theme.DisplayName);
            return false;
        }
    }
}
