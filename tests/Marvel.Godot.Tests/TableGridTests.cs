using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TableGridTests
{
    [Theory]
    [InlineData(1864, InterfaceScale.Percent100)]
    [InlineData(1864, InterfaceScale.Percent150)]
    [InlineData(2504, InterfaceScale.Percent100)]
    public void TwelveColumnsAndTheirGuttersFillTheActualWidth(float width, InterfaceScale scale)
    {
        var grid = TableGrid.For(width, scale);
        (float x, float span) = grid.Span(0, TableGrid.Columns);
        Assert.Equal(grid.Margin, x, 3);
        Assert.Equal(width - grid.Margin, x + span, 2);
    }

    [Fact]
    public void AWiderWindowWidensEveryColumnInsteadOfScalingAReferenceCanvas()
    {
        var narrow = TableGrid.For(1864, InterfaceScale.Percent100);
        var wide = TableGrid.For(2504, InterfaceScale.Percent100);
        Assert.Equal(narrow.Gutter, wide.Gutter);
        Assert.Equal((2504f - 1864f) / TableGrid.Columns, wide.Column - narrow.Column, 3);
    }

    [Fact]
    public void ALargerReadingScaleWidensTheGutters() =>
        Assert.True(TableGrid.For(1864, InterfaceScale.Percent150).Gutter > TableGrid.For(1864, InterfaceScale.Percent100).Gutter);

    [Fact]
    public void ASpanStartsAfterThePriorColumnsAndTheirGutters()
    {
        var grid = new TableGrid(1240, 8, 12);
        Assert.Equal(91, grid.Column, 3);
        Assert.Equal((8 + 2 * 103f, 3 * 91 + 2 * 12f), grid.Span(2, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Span(11, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Span(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Span(0, 0));
    }

    [Fact]
    public void ATooNarrowTableHasEmptyColumnsRatherThanNegativeOnes() => Assert.Equal(0, new TableGrid(100, 8, 12).Column);

    [Fact]
    public void DividingASpanKeepsOrderFillsItExactlyAndHonoursWeights()
    {
        var parts = TableGrid.Divide(100, 430, [1, 2, 1], 10);
        Assert.Equal([(100f, 102.5f), (212.5f, 205f), (427.5f, 102.5f)], parts);
        Assert.Equal(530, parts[^1].X + parts[^1].Width, 3);
        Assert.Throws<ArgumentException>(() => TableGrid.Divide(0, 10, [], 1));
        Assert.Throws<ArgumentException>(() => TableGrid.Divide(0, 10, [1, 0], 1));
    }
}
