using System.Drawing;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Core.Extensions;

/// <summary>
/// Extension methods for converting between System.Drawing types and Core model types.
/// </summary>
public static class DrawingExtensions
{
    /// <summary>
    /// Converts a System.Drawing.Rectangle to a framework-neutral IntRect.
    /// </summary>
    public static IntRect ToIntRect(this Rectangle rect) =>
        new(rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>
    /// Converts an IntRect to a System.Drawing.Rectangle.
    /// </summary>
    public static Rectangle ToRectangle(this IntRect rect) =>
        new(rect.X, rect.Y, rect.Width, rect.Height);
}
