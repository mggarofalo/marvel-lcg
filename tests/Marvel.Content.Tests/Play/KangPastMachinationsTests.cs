using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Past Machinations (11029) and the full authored book on a nonpublic Rhino
// board; Kang's public admission stays closed.
public sealed class KangPastMachinationsTests : CoreCardTestBase
{
    private const string PastMachinations = "11029";
    private const string StolenMemories = "11019";
    private const string Depowered = "11020";
    private const string Weakened = "11018";

    [Fact]
    [Rule("rr:each-player")]
    [Rule("rr:search")]
    [Rule("rr:incite-x.1")]
    public void EachPlayerRevealsAnObligationWhoseTitleNoEarlierPlayerRevealed()
    {
        // 11029: "Each player searches the encounter deck and discard pile for a
        // different obligation and reveals it." A different obligation has a title
        // no earlier player revealed in this resolution (MARVEL-894). Player 0
        // reveals one Stolen Memories; player 1 is offered neither copy, only
        // Depowered from the deck and Weakened from the discard pile. Incite 1
        // places 1 threat on the main scheme (rr:incite-x.1).
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 3);
        var memories = Deck(world, StolenMemories);
        var copy = Deck(world, StolenMemories);
        var depowered = Deck(world, Depowered);
        var weakened = world.CreateCard(Weakened, world.AreaOf(DeckType.EncounterDiscardPile));
        long threat = f.Main.Tokens.GetValueOrDefault("k_threat");
        var offered = new Dictionary<int, int[]>();

        var source = f.Reveal(PastMachinations, player: 1);
        f.Finish(prompt =>
        {
            if (RevealOrder(prompt) is { } incite) return incite;
            offered[prompt.Player] = [.. prompt.Affordances.Select(option => option.AnchorId).Order()];
            var chosen = prompt.Player == 0 ? memories : weakened;
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == chosen.ObjectId).Id);
        });

        Assert.Equal(new[] { memories, copy, depowered, weakened }.Select(card => card.ObjectId).Order(), offered[0]);
        Assert.Equal(new[] { depowered, weakened }.Select(card => card.ObjectId).Order(), offered[1]);
        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)), memories.Area);
        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)), weakened.Area);
        Assert.Equal(DeckType.EncounterDeck, copy.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, depowered.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(threat + 1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:each-player.2")]
    public void APlayerWhoFindsOnlyARevealedTitleRevealsNothing()
    {
        // Only two copies of one title exist. Player 0 reveals one; the other is
        // not a different obligation, so player 1 resolves nothing and is not asked.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var first = Deck(world, StolenMemories);
        var second = Deck(world, StolenMemories);
        var askers = new List<int>();

        f.Reveal(PastMachinations);
        f.Finish(prompt =>
        {
            if (RevealOrder(prompt) is { } incite) return incite;
            askers.Add(prompt.Player);
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == first.ObjectId).Id);
        });

        Assert.Equal([0], askers);
        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)), first.Area);
        Assert.Equal(DeckType.EncounterDeck, second.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:search")]
    public void WithNoObligationToFindNothingIsRevealed()
    {
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 4);
        long threat = f.Main.Tokens.GetValueOrDefault("k_threat");

        var source = f.Reveal(PastMachinations);
        f.Finish(prompt => RevealOrder(prompt) ?? throw new InvalidOperationException(prompt.Description));

        Assert.Empty(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)).Cards);
        Assert.Empty(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards);
        Assert.Equal(4, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(threat + 1, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:each-player.1")]
    public void TheFirstPlayerOrdersThePlayers()
    {
        // Ruling f9a6f072c87c1d0e: without a printed order the players decide as a
        // group, and the first player decides if they cannot agree; the engine asks
        // the first player (rr:each-player.1). Player 1 goes first and takes the
        // only Depowered, so player 0 may take only Weakened.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var depowered = Deck(world, Depowered);
        var weakened = Deck(world, Weakened);
        var askers = new List<int>();

        f.Reveal(PastMachinations);
        KangTemporalFixture.Drive(world, f.Runner, prompt =>
        {
            if (prompt.Affordances is [{ Verb: "Order", Targets: { } targets } order])
            {
                Assert.Equal(world.FirstPlayer, prompt.Player);
                return new Decision(order.Id, [.. targets.Legal.Reverse()]);
            }
            if (RevealOrder(prompt) is { } incite) return incite;
            if (prompt.Cancellable) return null;
            askers.Add(prompt.Player);
            var chosen = prompt.Player == 1 ? depowered : weakened;
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == chosen.ObjectId).Id);
        });

        Assert.Equal([1, 0], askers);
        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)), depowered.Area);
        Assert.Equal(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)), weakened.Area);
    }

    // Incite and the When Revealed ability share a timing; the first player
    // orders them (rr:simultaneous-resolution) and these tests resolve incite's
    // offered order as given.
    private static Decision? RevealOrder(Prompt prompt) =>
        prompt.Affordances.Count > 1 && prompt.Affordances.All(option => option.Verb == "Resolve")
            ? Decision.Take(prompt.Affordances[0].Id)
            : null;

    private static Card Deck(World world, string face) =>
        world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
}
