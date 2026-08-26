using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Standard set, which nearly every scenario in the game uses.
/// </summary>
/// <remarks>
/// <para>
/// Worth its own file because of how far these cards reach. Across the 135
/// campaigns in the dataset, "Shadow of the Past" appears in 132 and
/// "Exhaustion", "Masterplan" and "Under Fire" in 75 each — no other encounter
/// card in the pool comes close. A scenario that could not resolve them could
/// not be played at all.
/// </para>
/// </remarks>
public sealed class StandardSetCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:exhausted")]
    [Rule("rr:you-your")]
    [Fact]
    public void ExhaustionExhaustsTheRevealingPlayersIdentity()
    {
        // "Exhaust your identity card." The second player's, on a board with
        // two, so that "your" is a claim rather than a coincidence.
        var world = Deal("spider_man", "she_hulk");

        Reveal(world, AuthoredCards.Exhaustion, player: 1);

        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(world.Seats[1].IdentityCard.Ready);
    }

    [Rule("rr:exhausted")]
    [Fact]
    public void ExhaustingAnAlreadyExhaustedIdentityReportsNothing()
    {
        // Exhausted is a state and not a counter, so exhausting twice is not
        // two exhaustions -- and must not be two events, because the digest is
        // built from them.
        var world = Deal();
        world.Seats[0].IdentityCard.Exhaust();

        var events = Reveal(world, AuthoredCards.Exhaustion);

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Empty(events);
    }

    [Rule("rr:reveal")]
    [Fact]
    public void UnderFireRevealsTheTopCardRatherThanDealingIt()
    {
        // **Revealed, not dealt.** `rr:deal-deal-an-encounter-card` puts a card
        // facedown in a queue that the *next* villain phase resolves; this one
        // is turned over now. The difference is a whole phase, and the card
        // says "reveal".
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var top = deck.Cards[^1];
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Reveal(world, AuthoredCards.UnderFire);

        // Off the deck, on its way through a reveal step, and not in the queue.
        Assert.Equal(DeckType.RevealingArea, top.Area.Type);
        Assert.Equal(
            queued,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);

        var scheduled = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.RevealEncounterCard, scheduled.What);
        Assert.Equal(top.ObjectId, scheduled.Subject);
        Assert.Equal(0, scheduled.Seat);
    }

    [Rule("rr:encounter-deck")]
    [Fact]
    public void UnderFireOnAnEmptyDeckReshufflesRatherThanDoingNothing()
    {
        // The deck can be empty when this resolves, and `EncounterDeck.TakeTop`
        // is what makes that a reshuffle rather than a silent no-op.
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, discard);
        }

        Reveal(world, AuthoredCards.UnderFire);

        Assert.NotEmpty(deck.Cards);
        Assert.Single(world.Agenda.Outstanding);
    }

    private static IReadOnlyList<Marvel.Rules.Events.GameEvent> Reveal(
        World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        return AuthoredCards.Runner().WhenRevealed(world, card, player);
    }

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }
}
