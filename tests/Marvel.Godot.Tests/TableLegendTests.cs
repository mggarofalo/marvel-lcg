using Xunit;

namespace Marvel.Godot.Tests;

// The legend strip shows once per game until closed; no other test touches its state.
public sealed class TableLegendTests
{
    [Fact]
    public void TheLegendStripShowsOnceAndStaysClosedForTheGame()
    {
        TableLegend.Clear();
        Assert.True(TableLegend.OpensNow());
        Assert.True(TableLegend.OpensNow());
        TableLegend.Closed();
        Assert.False(TableLegend.OpensNow());
        Assert.False(TableLegend.OpensNow());
        TableLegend.Clear();
        Assert.True(TableLegend.OpensNow());
        TableLegend.Clear();
    }

    [Fact]
    public void TheLegendListsEachColumnIconOnce() =>
        Assert.Equal(TableLegend.Facts.Length, TableLegend.Facts.Select(fact => TableWording.For(fact).Icon).Distinct().Count());
}
