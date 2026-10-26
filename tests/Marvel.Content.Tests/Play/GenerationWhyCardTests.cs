using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual research face and full authored book; public admission stays closed.
public sealed class GenerationWhyCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:hazard-icon")]
    public void RevealUsesTheGlobalQuotaAndLeavesThePrintedSchemeInPlay(int resolver)
    {
        // 05026: "for each ally and Persona support in play"; two of each.
        // Hazard: "deal one player one additional card" in player order.
        var f = Create(6, eligible: true);
        var world = f.World;
        world.FirstPlayer = 1;
        var originals = world.Seats.Select(seat => seat.Deck.Cards.ToArray()).ToArray();
        var scheme = f.Reveal("05026", resolver);

        var events = f.Finish();

        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(4, scheme.Tokens["k_threat"]);
        Assert.Equal(1, Deal.HazardIcons(world, Cards));
        Assert.Equal(3, Cards.PrintedValue(scheme.FaceId, "Boost", world.Players));
        for (int player = 0; player < 2; player++)
        {
            Assert.Equal(4, Discarded(events, player));
            Assert.Equal(2, world.Seats[player].Deck.Cards.Count);
            Assert.Equal(originals[player].TakeLast(4).OrderBy(card => card.ObjectId),
                world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player)).Cards.OrderBy(card => card.ObjectId));
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, world.Result);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.DealEncounterCards), 2, 3));
        f.Finish();
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(2, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    [Rule("rr:player-deck.3")]
    public void ShortDeckResetsAndDealsAnEncounterWithoutChangingTheOtherPlayersQuota()
    {
        // "no further cards are discarded from the newly shuffled deck";
        // the player "immediately deals themself one facedown encounter card".
        // ruling:e8654dd34b58be12 fixes 05026's count at initiation.
        var f = Create(6, eligible: true);
        var world = f.World;
        foreach (var card in world.Seats[0].Deck.Cards.Skip(2).ToArray())
            World.MoveToTop(card, world.Seats[0].Hand);
        var original = world.Seats[0].Deck.Cards.ToArray();
        var scheme = f.Reveal("05026", 1);

        var events = f.Finish();

        Assert.Equal(2, Discarded(events, 0));
        Assert.Equal(4, Discarded(events, 1));
        Assert.Equal(original.OrderBy(card => card.ObjectId), world.Seats[0].Deck.Cards.OrderBy(card => card.ObjectId));
        Assert.Empty(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(2, world.Seats[1].Deck.Cards.Count);
        Assert.Equal(4, scheme.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void NoEligibleCardsStillRevealsTheSchemeWithoutDiscardOrRandomConsumption()
    {
        var f = Create(6, eligible: false);
        var world = f.World;
        var decks = world.Seats.Select(seat => seat.Deck.Cards.ToArray()).ToArray();
        var random = world.Random.Generator.GetState();
        var scheme = f.Reveal("05026", 1);

        var events = f.Finish();

        Assert.Equal(0, Discarded(events, 0));
        Assert.Equal(0, Discarded(events, 1));
        Assert.Equal(decks[0], world.Seats[0].Deck.Cards);
        Assert.Equal(decks[1], world.Seats[1].Deck.Cards);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(4, scheme.Tokens["k_threat"]);
        Assert.Equal(1, Deal.HazardIcons(world, Cards));
        Assert.False(world.Agenda.IsBusy);
    }

    private static int Discarded(IEnumerable<GameEvent> events, int player) =>
        events.OfType<CardsMoved>().Where(item => item.Verb == "Discard"
            && item.From.Zone == "PlayerDeck" && item.From.Owner == player).Sum(item => item.Cards.Count);

    private static RedSkullFixture Create(int deckSize, bool eligible)
    {
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        foreach (var seat in world.Seats)
        {
            world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index));
            world.CreateCard("05007", seat.Hand);
            for (int index = 0; index < deckSize; index++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01002", eligible ? world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)) : world.Seats[0].Hand);
        world.CreateCard("01084", eligible ? world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)) : world.Seats[1].Hand);
        world.CreateCard("05006", eligible ? world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)) : world.Seats[0].Hand);
        world.CreateCard("05008", eligible ? world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)) : world.Seats[1].Hand);
        for (int index = 0; index < 8; index++) world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        return new(world, runner);
    }
}
