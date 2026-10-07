using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardNumberGeometryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EntireConsequenceGroupCentersUnderMeasuredNumeral(int count)
    {
        var numeral = new Rect2(37, 12, 23, 28);
        var geometry = new CardNumberGeometry(numeral, 34, new(false, false, count), 1, true);

        Assert.Equal(count, geometry.Consequences.Count);
        if (count == 0) return;
        Rect2 group = geometry.Consequences.Aggregate((left, right) => left.Merge(right));
        Assert.Equal(numeral.GetCenter().X, group.GetCenter().X);
        Assert.All(geometry.Consequences, mark => Assert.True(mark.Position.Y > geometry.Underline.End.Y));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(23)]
    [InlineData(35)]
    public void SpecialStarFollowsActualValueWidthWithoutChangingItsCenter(float width)
    {
        var numeral = new Rect2(37, 12, width, 28);
        var geometry = new CardNumberGeometry(numeral, 34, new(true, false, 3), 1, true);

        Assert.Equal(numeral.End.X + 0.5f, geometry.Special.Position.X);
        Assert.Equal(numeral.Position.Y, geometry.Special.Position.Y);
        Assert.False(geometry.Number.Intersects(geometry.Special));
        Assert.Equal(numeral.GetCenter().X, geometry.Consequences[1].GetCenter().X);
    }

    [Fact]
    public void PerPlayerAndSpecialMarksHaveSeparateSlots()
    {
        var geometry = new CardNumberGeometry(new Rect2(0, 0, 15, 22), 18,
            new(true, true, 2), 0.8f, false);

        Assert.False(geometry.Special.Intersects(geometry.PerPlayer));
        Assert.True(geometry.PerPlayer.Position.X > geometry.Special.End.X);
        Assert.Equal(2, geometry.Consequences.Count);
    }
}
