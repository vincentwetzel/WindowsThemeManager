namespace WindowsThemeManager.Core.Models;

/// <summary>
/// A framework-neutral rectangle struct for geometry calculations.
/// Replaces System.Drawing.Rectangle in the Core library to avoid UI framework dependencies.
/// </summary>
public readonly struct IntRect : IEquatable<IntRect>
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool IsEmpty => Width == 0 || Height == 0;

    public IntRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static IntRect FromLTRB(int left, int top, int right, int bottom) =>
        new(left, top, right - left, bottom - top);

    public static IntRect Empty => new(0, 0, 0, 0);

    /// <summary>
    /// Returns a rectangle encompassing all provided rectangles.
    /// </summary>
    public static IntRect Union(params IntRect[] rects)
    {
        if (rects.Length == 0) return Empty;

        var minX = rects.Min(r => r.Left);
        var minY = rects.Min(r => r.Top);
        var maxX = rects.Max(r => r.Right);
        var maxY = rects.Max(r => r.Bottom);

        return FromLTRB(minX, minY, maxX, maxY);
    }

    public bool Contains(int x, int y) =>
        x >= Left && x < Right && y >= Top && y < Bottom;

    public bool Contains(IntRect other) =>
        Left <= other.Left && Top <= other.Top && Right >= other.Right && Bottom >= other.Bottom;

    /// <summary>
    /// Intersects this rectangle with another.
    /// </summary>
    public IntRect Intersect(IntRect other)
    {
        var left = Math.Max(Left, other.Left);
        var top = Math.Max(Top, other.Top);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        if (left >= right || top >= bottom)
            return Empty;

        return FromLTRB(left, top, right, bottom);
    }

    public override string ToString() => $"{{X={X},Y={Y},Width={Width},Height={Height}}}";

    public bool Equals(IntRect other) =>
        X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    public override bool Equals(object? obj) => obj is IntRect other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    public static bool operator ==(IntRect left, IntRect right) => left.Equals(right);
    public static bool operator !=(IntRect left, IntRect right) => !left.Equals(right);
}
