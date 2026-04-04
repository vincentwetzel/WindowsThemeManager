using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.Tests.Unit;

public class IntRectTests
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var rect = new IntRect(10, 20, 100, 200);

        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void FromLTRB_CreatesCorrectRectangle()
    {
        var rect = IntRect.FromLTRB(10, 20, 110, 220);

        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void Empty_IsEmpty()
    {
        Assert.True(IntRect.Empty.IsEmpty);
        Assert.True(new IntRect(0, 0, 0, 50).IsEmpty);
        Assert.False(new IntRect(0, 0, 100, 100).IsEmpty);
    }

    [Fact]
    public void Union_CombinesRectangles()
    {
        var r1 = new IntRect(0, 0, 100, 100);
        var r2 = new IntRect(50, 50, 100, 100);

        var union = IntRect.Union(r1, r2);

        Assert.Equal(0, union.X);
        Assert.Equal(0, union.Y);
        Assert.Equal(150, union.Width);
        Assert.Equal(150, union.Height);
    }

    [Fact]
    public void Union_EmptyArray_ReturnsEmpty()
    {
        Assert.True(IntRect.Union(Array.Empty<IntRect>()).IsEmpty);
    }

    [Fact]
    public void Contains_Point()
    {
        var rect = new IntRect(10, 10, 100, 100);

        Assert.True(rect.Contains(50, 50));
        Assert.True(rect.Contains(10, 10));
        Assert.False(rect.Contains(110, 10));
        Assert.False(rect.Contains(5, 5));
    }

    [Fact]
    public void Intersect_OverlappingRectangles()
    {
        var r1 = new IntRect(0, 0, 100, 100);
        var r2 = new IntRect(50, 50, 100, 100);

        var intersection = r1.Intersect(r2);

        Assert.Equal(50, intersection.X);
        Assert.Equal(50, intersection.Y);
        Assert.Equal(50, intersection.Width);
        Assert.Equal(50, intersection.Height);
    }

    [Fact]
    public void Intersect_NonOverlapping_ReturnsEmpty()
    {
        var r1 = new IntRect(0, 0, 50, 50);
        var r2 = new IntRect(100, 100, 50, 50);

        Assert.True(r1.Intersect(r2).IsEmpty);
    }

    [Fact]
    public void Equality_Works()
    {
        var r1 = new IntRect(10, 20, 100, 200);
        var r2 = new IntRect(10, 20, 100, 200);
        var r3 = new IntRect(0, 0, 100, 200);

        Assert.Equal(r1, r2);
        Assert.True(r1 == r2);
        Assert.NotEqual(r1, r3);
        Assert.True(r1 != r3);
    }

    [Fact]
    public void ToString_Formatted()
    {
        var rect = new IntRect(10, 20, 100, 200);
        var str = rect.ToString();

        Assert.Contains("10", str);
        Assert.Contains("20", str);
        Assert.Contains("100", str);
        Assert.Contains("200", str);
    }
}
