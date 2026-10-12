using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TimelineViewportTests
{
    [Fact]
    public void LongHistoryPaintsOnlyPixelBoundedBinsWithoutDiscardingOccurrences()
    {
        var viewport = new TimelineViewport();
        viewport.SetCount(100_000);
        viewport.Zoom(100_000, .5);
        var bins = viewport.Bins(600).ToArray();
        Assert.InRange(bins.Length, 1, 61);
        Assert.Equal(0, bins[0].First);
        Assert.Equal(99_999, bins[^1].Last);
        viewport.Select(54_321);
        Assert.Equal(54_321, viewport.Selected);
    }

    [Fact]
    public void NewEventsDoNotStealThePlaceBeingBrowsedAndLatestResumesFollowing()
    {
        var viewport = new TimelineViewport();
        viewport.SetCount(100);
        viewport.Select(30);
        double left = viewport.Left;
        viewport.SetCount(120);
        Assert.Equal(30, viewport.Selected);
        Assert.Equal(left, viewport.Left);
        viewport.Latest();
        viewport.SetCount(121);
        Assert.Equal(120, viewport.Selected);
        Assert.True(viewport.Following);
    }

    [Fact]
    public void ZoomPreservesTheHoveredOccurrenceAndUndoClampsSelection()
    {
        var viewport = new TimelineViewport();
        viewport.SetCount(100);
        viewport.Pan(-30);
        int hovered = viewport.At(.4);
        viewport.Zoom(.7, .4);
        Assert.Equal(hovered, viewport.At(.4));
        viewport.Select(90);
        viewport.SetCount(12);
        Assert.Equal(11, viewport.Selected);
        viewport.Select(-10);
        Assert.Equal(0, viewport.Selected);
        viewport.SetCount(0);
        Assert.Equal(-1, viewport.At(.5));
        Assert.Empty(viewport.Bins(600));
    }
}
