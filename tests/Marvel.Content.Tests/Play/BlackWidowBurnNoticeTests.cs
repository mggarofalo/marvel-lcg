using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowBurnNoticeTests
{
    [Theory]
    [Rule("rr:obligation.1")]
    [InlineData(false)]
    [InlineData(true)]
    public void BurnNoticeGivenToNonfirstWidowCanFlipExhaustAndRemove(bool exhausted)
    {
        // Obligation "you" applies "only to the player whose play area the obligation is in".
        var world = WidowEncounterFixture.Board();
        if (exhausted) world.Seats[1].IdentityCard.Exhaust();
        var runner = WidowEncounterFixture.Runner();
        var f = new RedSkullFixture(world, runner);
        var notice = f.Reveal("08025", 0);
        var flip = Work(world);
        Assert.Equal(1, flip.Player);
        Assert.Equal(PlayArea.Of(1), notice.Area.PlayArea);
        Answer(world, flip, 0);
        var consequence = Work(world);
        Assert.Equal("08001b", world.Seats[1].IdentityCard.FaceId);
        Assert.Equal(!exhausted, consequence.Affordances.Any(a => a.Id == 0));
        Answer(world, consequence, exhausted ? 1 : 0);
        f.Finish();
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(exhausted ? DeckType.EncounterDiscardPile : DeckType.RemovedArea, notice.Area.Type);
        Assert.Equal(exhausted ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
    }

    [Fact]
    public void BurnNoticeRanksOnlyOwnPreparationsAndFirstPlayerBreaksTie()
    {
        var world = WidowEncounterFixture.Board();
        var runner = WidowEncounterFixture.Runner();
        var low = WidowEncounterFixture.Upgrade(world, "08010");
        var high = WidowEncounterFixture.Upgrade(world, "08008");
        var tie = WidowEncounterFixture.Upgrade(world, "08008");
        var ordinary = WidowEncounterFixture.Upgrade(world, "01002");
        var foreign = WidowEncounterFixture.Upgrade(world, "08008", 0);
        var f = new RedSkullFixture(world, runner);
        var notice = f.Reveal("08025", 0);
        Answer(world, Work(world), 1);
        var alternatives = Work(world);
        Assert.DoesNotContain(alternatives.Affordances, a => a.Id == 0);
        Answer(world, alternatives, 1);
        var choice = Work(world);
        Assert.Equal(0, choice.Player);
        Assert.Equal(new[] {high.ObjectId, tie.ObjectId}.Order(), choice.Affordances.Select(a => a.Id).Order());
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => Answer(world, choice, foreign.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        Answer(world, choice, tie.ObjectId);
        f.Finish();
        Assert.Equal(DeckType.DiscardPile, tie.Area.Type);
        Assert.All(new[] {low, high, ordinary, foreign}, c => Assert.Equal(DeckType.UpgradesArea, c.Area.Type));
        Assert.Equal(DeckType.EncounterDiscardPile, notice.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.True(world.Seats[1].IdentityCard.Ready);
    }

    [Fact]
    public void AddedRowsHaveExactInventoryAndNoStartingCounters()
    {
        using var book = System.Text.Json.JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(685, rows.Length);
        string[] added = ["08023", "08025", "08026", "08029"];
        Assert.Equal(added,
            rows.Skip(628).Take(4).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(628).Take(4), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("11028", AuthoredCards.Book.Authored);
        string[] seven = ["08003", "08004", "08013", "08012", "08018", "08024", "08033"];
        Assert.Equal(seven,
            rows.Skip(632).Take(7).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(632).Take(7), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
    }

    private static Prompt Work(World world) => Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities, []));
    private static void Answer(World world, Prompt prompt, int id) =>
        Sequence.Answer(world, world.Facts, world.Abilities, prompt, Decision.Take(id), []);
}


