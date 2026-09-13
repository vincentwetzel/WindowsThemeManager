using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Interfaces;

/// <summary>
/// Service responsible for applying complete Windows theme files.
/// </summary>
public interface IThemeApplier
{
    /// <summary>
    /// Applies a complete theme through the Windows theme shell workflow.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the theme was applied successfully.</returns>
    Task<bool> ApplyThemeAsync(Theme theme, CancellationToken cancellationToken = default);

}
