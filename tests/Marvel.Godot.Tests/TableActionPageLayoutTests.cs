using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TableActionPageLayoutTests
{
    [Fact]
    public void CompleteMeasuredRowsAppearExactlyOnceAcrossPages()
    {
        float[] rows = [44, 62, 44, 51, 88, 44];
        TableActionPageLayout layout = TableActionPageLayout.Measure(rows, 190, 44, 8, 6);
        Assert.False(layout.RequiresCompleteChoices);
        Assert.Equal(Enumerable.Range(0, rows.Length), layout.Pages.SelectMany(
            page => Enumerable.Range(page.Start, page.Count)));
        Assert.All(layout.Pages, page => Assert.True(
            rows.Skip(page.Start).Take(page.Count).Sum() + (page.Count - 1) * 8 <= 140));
    }

    [Fact]
    public void NavigationConsumesItsMeasuredHeightBeforeRowsArePacked()
    {
        TableActionPageLayout ordinary = TableActionPageLayout.Measure([44, 44, 44, 44], 152, 44, 8, 8);
        TableActionPageLayout largerNavigation = TableActionPageLayout.Measure([44, 44, 44, 44], 152, 64, 8, 8);
        Assert.Equal(new TableActionPage(0, 2), ordinary.Pages[0]);
        Assert.All(largerNavigation.Pages, page => Assert.Equal(1, page.Count));
    }

    [Fact]
    public void FittingAllRowsDoesNotReserveUnneededNavigation()
    {
        TableActionPageLayout layout = TableActionPageLayout.Measure([44, 44], 96, 44, 8, 8);
        Assert.False(layout.RequiresCompleteChoices);
        Assert.Equal(new TableActionPage(0, 2), Assert.Single(layout.Pages));
    }

    [Theory]
    [InlineData(200, 100)]
    [InlineData(44, 80)]
    public void OversizedRowsUseCompleteChoicesInsteadOfPartialOrEndlessPages(float row, float available)
    {
        TableActionPageLayout layout = TableActionPageLayout.Measure([row, 44], available, 44, 8, 8);
        Assert.True(layout.RequiresCompleteChoices);
        Assert.Empty(layout.Pages);
        Assert.Equal(0, layout.ClampPage(500));
    }

    [Fact]
    public void ContextResizingAndRowChangesRepartitionAndClampTheCurrentPage()
    {
        TableActionPageLayout compact = TableActionPageLayout.Measure([44, 44, 44, 44], 120, 44, 8, 8);
        Assert.Equal(4, compact.Pages.Count);
        TableActionPageLayout expanded = TableActionPageLayout.Measure([44, 62, 44, 44], 200, 44, 8, 8);
        Assert.Equal(1, expanded.ClampPage(3));
        TableActionPageLayout replaced = TableActionPageLayout.Measure([62], 120, 44, 8, 8);
        Assert.Equal(0, replaced.ClampPage(1));
        Assert.Equal(0, replaced.ClampPage(-1));
    }
}
