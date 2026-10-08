using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardLiveStateTests
{
    [Fact]
    public void ConcealedSnapshotsNeverYieldStateEvenIfAnInconsistentCallerSuppliesIt()
    {
        var card = Card() with { Concealed = true, Title = "Concealed", Damage = 2,
            Status = "EXHAUSTED", Statuses = ["Stunned"], Counters = [new("Uses", "4")],
            Retaliate = 3, Fields = [new("HEALTH", "5/7")], PrintedStats = [new("Guard", "1")] };
        Assert.Empty(CardStatusEntries.From(card));
        Assert.Equal("Concealed", CardLiveStateRendering.Description(card));
    }

    [Fact]
    public void RepeatedStatusCardsRetainTheirCountAndAReplacementSnapshotRemovesThem()
    {
        var card = Card() with { Statuses = ["Stunned", "STUNNED", "Tough"],
            Counters = [new("ARROW", "12")], Retaliate = 2 };
        Assert.Contains(CardStatusEntries.From(card), row => row.Text == "2 × Stunned");
        Assert.Contains(CardStatusEntries.From(card), row => row.Text == "Tough");
        Assert.Contains(CardStatusEntries.From(card), row => row.Text == "12 Arrow");
        Assert.Contains(CardStatusEntries.From(card), row => row.Text == "Retaliate 2");
        Assert.Empty(CardStatusEntries.From(Card()));
    }

    [Fact]
    public void LiveZeroSuppressesPrintedIconsAndNamesDoNotBecomeResourceSymbols()
    {
        var card = Card() with { Fields = [new("HAZARD", "0"), new("GUARD", "1"), new("ACCELERATION ICON", "1")],
            PrintedStats = [new("Hazard", "2"), new("Acceleration", "1")] };
        var rows = CardStatusEntries.From(card);
        Assert.DoesNotContain(rows, row => row.Name == "Hazard");
        Assert.Contains(rows, row => row.Text == "Guard");
        Assert.Contains(rows, row => row.Text == "Acceleration");
    }

    [Fact]
    public void AccessibleStateKeepsRemainingMaximumAndDamageSeparate()
    {
        string description = CardLiveStateRendering.Description(Card() with
        { Fields = [new("HEALTH", "5/7")], Damage = 2, Statuses = ["Tough"] });
        Assert.Contains("Hit points 5/7", description);
        Assert.Contains("2 damage", description);
        Assert.Contains("Tough", description);
    }

    private static BoardCardPresentation Card() => new(1, 1, false, "Drone", "", "MINION", "READY", []);
}
