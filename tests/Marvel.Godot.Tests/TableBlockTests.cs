using Xunit;

namespace Marvel.Godot.Tests;

// The building blocks' decisions are pure; the native Kang contracts check the controls they build.
public sealed class TableBlockTests
{
    [Fact]
    public void EveryFactIconIsAnEmbeddedTableIcon()
    {
        string[] embedded = typeof(TableWording).Assembly.GetManifestResourceNames();
        foreach (TableFact fact in Enum.GetValues<TableFact>())
            Assert.Contains($"Marvel.Godot.Assets.TableIcons.{TableWording.For(fact).Icon}.svg", embedded);
    }

    [Theory]
    [InlineData("tough", 8, false)]
    [InlineData("STUNNED", 16, false)]
    [InlineData("Confused", 0, true)]
    public void AStatusBadgeIsToldApartByShape(string status, int points, bool ring)
    {
        Assert.True(StatusBadge.Supports(status));
        Assert.Equal((points, ring), StatusBadge.Outline(status));
    }

    [Fact]
    public void AStatusWithoutABadgeIsRefused()
    {
        Assert.False(StatusBadge.Supports("Engaged"));
        Assert.Throws<ArgumentOutOfRangeException>(() => StatusBadge.Outline("Engaged"));
    }

    [Fact]
    public void AChipShowsItsShortLabelAndItsCount()
    {
        Wording wording = TableWording.For(TableFact.StoredDominions);
        Assert.Equal("Dominions", Chip.Text(wording, null));
        Assert.Equal("Dominions 2", Chip.Text(wording, 2));
    }

    [Theory]
    [InlineData(2, 3, new[] { true, true, false })]
    [InlineData(0, 2, new[] { false, false })]
    [InlineData(5, 2, new[] { true, true })]
    [InlineData(-1, 1, new[] { false })]
    [InlineData(1, 0, new bool[0])]
    public void ATrackerFillsItsPipsFirst(int filled, int total, bool[] expected) =>
        Assert.Equal(expected, Tracker.Pips(filled, total));

    [Fact]
    public void ATrackerReadsAsProgressAndItsCount()
    {
        Wording dominions = TableWording.For(TableFact.StoredDominions);
        Assert.Equal("Kang (II) 2 of 3, Dominions 1", Tracker.Reading(new("Kang (II)", 2, 3, dominions, 1, "")));
        Assert.Equal("Kang (III)", Tracker.Reading(new("Kang (III)", 0, 0, null, 0, "")));
    }

    [Fact]
    public void AColumnHeaderPublishesItsFactsInOrder() => Assert.Equal(["YourArea", "Acting"],
        ColumnHeader.Keys([TableFact.YourArea, TableFact.Acting]));

    [Fact]
    public void AColumnHeaderIsOneLineTallAndGrowsWithTheReadingScale()
    {
        Assert.True(ColumnHeader.Height(InterfaceScale.Percent150) > ColumnHeader.Height(InterfaceScale.Percent100));
        Assert.True(ColumnHeader.Height(InterfaceScale.Percent100) < 2 * VisualSystem.Type(InterfaceScale.Percent100).Heading);
    }

    [Fact]
    public void APanelReasonIsOneLine()
    {
        Assert.Equal("Choose a card · It goes on top.", TablePanel.OneLine("Choose a card\n It goes on top. \n"));
        Assert.Equal("", TablePanel.OneLine("  \n "));
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(3, "+3 ▸")]
    public void AnOverflowCueCountsWhatIsHidden(int hidden, string text) => Assert.Equal(text, OverflowCue.Text(hidden));

    [Theory]
    [InlineData(4, 60, 250, 0)]
    [InlineData(4, 60, 179, 2)]
    [InlineData(4, 0, 10, 0)]
    [InlineData(2, 60, 0, 2)]
    public void AnOverflowCueHidesWhatAViewportCannotShow(int count, float row, float viewport, int hidden) =>
        Assert.Equal(hidden, OverflowCue.Hidden(count, row, viewport));
}
