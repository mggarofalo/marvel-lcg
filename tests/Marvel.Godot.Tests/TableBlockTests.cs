using Xunit;

namespace Marvel.Godot.Tests;

// The building blocks' decisions are pure; the native Kang contracts check the controls they build.
public sealed class TableBlockTests
{
    [Fact]
    public void EveryFactIconIsAnEmbeddedTableIcon()
    {
        string[] embedded = typeof(TableWording).Assembly.GetManifestResourceNames();
        foreach (TableFact fact in Enum.GetValues<TableFact>().Where(fact => TableWording.For(fact).Icon.Length > 0))
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
    public void ASheetsReasonKeepsTheWholeInstruction()
    {
        const string instruction = "Choose the order.\nResolve it completely, including its whole-group join, before the other departures.";
        Assert.Equal(instruction, TablePanel.ReasonText($" {instruction} \n", wrap: true));
        Assert.Equal(TablePanel.OneLine(instruction), TablePanel.ReasonText(instruction, wrap: false));
    }

    [Theory]
    [InlineData(1, true, "")]
    [InlineData(2, true, "×2")]
    [InlineData(2, false, "2 × Stunned")]
    public void ACompactStatusBadgeShowsOnlyACountAboveOne(int count, bool compact, string caption) =>
        Assert.Equal(caption, StatusBadge.Caption("2 × Stunned", count, compact));

    [Theory]
    [InlineData("Tough", 8, false)]
    [InlineData("stunned", 16, false)]
    [InlineData("CONFUSED", 0, true)]
    public void EachStatusHasItsOwnShape(string status, int points, bool ring) =>
        Assert.Equal((points, ring), StatusBadge.Outline(status));

    [Fact]
    public void AStatusBadgeIsTwoBodyLinesAtTheReadingScale()
    {
        Assert.Equal(28, StatusBadge.Side(InterfaceScale.Percent100));
        Assert.True(StatusBadge.Side(InterfaceScale.Percent150) > StatusBadge.Side(InterfaceScale.Percent100));
        Assert.NotEqual(StatusBadge.Tint("Tough"), StatusBadge.Tint("Stunned"));
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
    [InlineData(0, 300, 0)]
    [InlineData(0, 179, 3)]
    [InlineData(0, 180.4f, 2)]
    [InlineData(60, 300, 1)]
    [InlineData(59.6f, 239.6f, 2)]
    [InlineData(60.6f, 300, 2)]
    public void AnOverflowCueCountsTheRowsAViewportDoesNotWhollyShow(float top, float bottom, int hidden)
    {
        // Rows of unequal height, as tiles grow when their badges wrap.
        (float, float)[] rows = [(0, 60), (60, 180), (180, 240), (240, 300)];
        Assert.Equal(hidden, OverflowCue.Hidden(rows, top, bottom));
    }
}
