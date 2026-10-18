using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckRowSelectionTests
{
    [Fact]
    public void ControlShiftAddsRangeFromMostRecentControlClick()
    {
        var selection = new DeckRowSelection();
        selection.Click(0, false, false);
        selection.Click(2, true, false);
        selection.Click(4, true, true);
        Assert.Equal([0, 2, 3, 4], selection.Selected);
        selection.Click(3, true, false);
        Assert.Equal([0, 2, 4], selection.Selected);
    }

    [Fact]
    public void PlainShiftReplacesSelectionAndKeepsTheRangeAnchor()
    {
        var selection = new DeckRowSelection();
        selection.Click(0, false, false);
        selection.Click(4, true, false);
        selection.Click(2, false, true);
        Assert.Equal([2, 3, 4], selection.Selected);
        selection.Click(1, false, true);
        Assert.Equal([1, 2, 3, 4], selection.Selected);
    }
}
