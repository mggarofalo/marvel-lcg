using Xunit;

namespace Marvel.Godot.Tests;

// The legend opens by itself once per game and then follows its toggle; no other test touches its state.
public sealed class TableLegendTests
{
    [Fact]
    public void TheLegendOpensOnceAndThenFollowsItsToggle()
    {
        TableLegend.Clear();
        Assert.True(TableLegend.OpensNow());
        Assert.True(TableLegend.OpensNow());
        TableLegend.Toggled(false);
        Assert.False(TableLegend.OpensNow());
        TableLegend.Toggled(true);
        Assert.True(TableLegend.OpensNow());
        TableLegend.Clear();
        Assert.True(TableLegend.OpensNow());
        TableLegend.Clear();
    }

    [Fact]
    public void TheLegendListsEachColumnIconOnce() =>
        Assert.Equal(TableLegend.Facts.Length, TableLegend.Facts.Select(fact => TableWording.For(fact).Icon).Distinct().Count());
}
