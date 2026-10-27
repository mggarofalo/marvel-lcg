using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic multiplayer boards isolate ordinary discard ownership.</summary>
public sealed class EliminatedOwnerDiscardTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [Rule("rr:ownership-and-control.7.2")]
    [Rule("rr:discard.1")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:player-deck.4")]
    [Rule("rr:ally-limit")]
    public void ForeignControlledAllyUsesItsOwnersDestinationAndReset(
        bool ownerEliminated, bool emptyOwnerDeck, bool throughDecision)
    {
        // Leaving play uses the owner's equivalent area, "or removed from the
        // game if that player is no longer in the game". An empty deck resets
        // "until there is at least one card in the player's discard pile".
        // Ally limit: "choose and discard from play ally cards they control".
        var world = Board();
        var chosen = world.CreateCard("ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 1));
        Elimination.Eliminate(world, world.Facts, ownerEliminated ? 1 : 2, "test", []);
        if (emptyOwnerDeck)
            World.MoveToTop(Assert.Single(world.Seats[1].Deck.Cards), world.Seats[1].Hand);
        Assert.Equal(1, chosen.Owner);
        Assert.Equal(PlayArea.Of(0), chosen.Area.PlayArea);
        Assert.Equal(ownerEliminated, world.Seats[1].Eliminated);
        int[] controllerDeck = [.. world.Seats[0].Deck.Cards.Select(card => card.ObjectId)];
        int[] encounterDeck = [.. world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId)];
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();

        if (throughDecision)
        {
            for (int count = 0; count < 2; count++)
                world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            var fourth = world.CreateCard("ally", world.Seats[0].Hand);
            CardPlay.PutAllyIntoPlay(world, world.Facts, abilities, fourth, 0, "test", events);
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, abilities, events));
            Assert.Equal(0, prompt.Player);
            Assert.Equal(Question.Element, prompt.Asking);
            Assert.False(prompt.Cancellable);
            Assert.Contains(prompt.Affordances, option => option.Id == chosen.ObjectId);
            Assert.Equal(DeckType.AlliesArea, chosen.Area.Type);
            Sequence.Answer(world, world.Facts, abilities, prompt, Decision.Take(chosen.ObjectId), events);
            Sequence.Finish(world, world.Facts, abilities, events);
        }
        else Discard.Card(world, chosen, "test", events);

        bool reset = !ownerEliminated && emptyOwnerDeck;
        AssertDestination(world, chosen, ownerEliminated, reset, events);
        Assert.Equal(reset ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Equal(reset ? encounterDeck[..^1] : encounterDeck,
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId));
        Assert.Equal(controllerDeck, world.Seats[0].Deck.Cards.Select(card => card.ObjectId));
        if (ownerEliminated) Assert.Empty(world.Seats[1].Deck.Cards);
        Assert.Equal(words, world.Random.Generator.WordsConsumed); // A one-card reset consumes no random words.
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ownership-and-control.7.2")]
    [Rule("rr:attach-to.1")]
    public void RemovingAnEliminatedOwnersHostStillDiscardsAndDetachesAnActiveOwnersUpgrade()
    {
        // "If the game element ... leaves play, the attachment is discarded."
        // The host is "removed from the game if that player is no longer in the game".
        var world = Board();
        var host = world.CreateCard("ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 1));
        var attached = world.CreateCard("upgrade",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 2));
        Elimination.Eliminate(world, world.Facts, 1, "test", []);
        var events = new List<GameEvent>();

        Discard.Card(world, host, "test", events);

        Assert.Equal(DeckType.RemovedArea, host.Area.Type);
        Assert.Equal(DeckType.DiscardPile, attached.Area.Type);
        Assert.Equal(PlayArea.Of(2), attached.Area.PlayArea);
        Assert.Contains(events.OfType<CardDetached>(), moved => moved.Card == attached.ObjectId && moved.Host == host.ObjectId);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), moved => moved.Verb == "Reset");
        Assert.Empty(world.Seats[1].Deck.Cards);
    }

    [Fact]
    [Rule("rr:player-elimination.step.4")]
    [Rule("rr:player-elimination.step.5")]
    public void EliminationDiscardsItsOwnInPlayCardsBeforeRemovingItsAreas()
    {
        // "Place each card owned by the eliminated player in the eliminated
        // player's discard pile", then "Remove the eliminated player's play area".
        var world = Board();
        var ally = world.CreateCard("ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 1, "test", events);

        Assert.Equal(DeckType.RemovedArea, ally.Area.Type);
        var moves = events.OfType<CardsMoved>()
            .Where(move => move.Cards.Any(card => card.Card == ally.ObjectId)).ToArray();
        Assert.Equal(2, moves.Length);
        Assert.Equal(nameof(DeckType.DiscardPile), moves[0].To.Zone);
        Assert.Equal(nameof(DeckType.DiscardPile), moves[1].From.Zone);
        Assert.Equal(nameof(DeckType.RemovedArea), moves[1].To.Zone);
        Assert.Empty(world.Seats[1].Deck.Cards);
    }

    [Fact]
    [Rule("rr:player-elimination.step.4")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:player-elimination.6")]
    public void EliminatingAnEmptyDeckSeatDoesNotResetOrConsumeAnEncounterCard()
    {
        // "Eliminated players no longer participate in the game"; "Effects that
        // refer to the players in the game ignore eliminated players". Step 4
        // places owned cards in their discard, then step 5 removes their areas.
        var world = Board();
        World.MoveToTop(Assert.Single(world.Seats[1].Deck.Cards), world.Seats[1].Hand);
        var ally = world.CreateCard("ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        int[] encounter = [.. world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId)];
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 1, "test", events);

        Assert.Equal(DeckType.RemovedArea, ally.Area.Type);
        Assert.Equal(encounter, world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId));
        Assert.DoesNotContain(events.OfType<CardsMoved>(), move => move.Verb == "Reset");
        Assert.DoesNotContain(events.OfType<AreaReordered>(), move => move.Verb == "Shuffle");
        var moves = events.OfType<CardsMoved>()
            .Where(move => move.Cards.Any(card => card.Card == ally.ObjectId)).ToArray();
        Assert.Equal(2, moves.Length);
        Assert.Equal(nameof(DeckType.DiscardPile), moves[0].To.Zone);
        Assert.Equal(nameof(DeckType.DiscardPile), moves[1].From.Zone);
        Assert.Equal(nameof(DeckType.RemovedArea), moves[1].To.Zone);
    }

    private static void AssertDestination(
        World world, Card chosen, bool ownerEliminated, bool reset, List<GameEvent> events)
    {
        Assert.Equal(ownerEliminated ? DeckType.RemovedArea : reset ? DeckType.PlayerDeck : DeckType.DiscardPile,
            chosen.Area.Type);
        if (!ownerEliminated) Assert.Equal(PlayArea.Of(1), chosen.Area.PlayArea);
        var move = Assert.Single(events.OfType<CardsMoved>(), moved =>
            moved.From.Zone == nameof(DeckType.AlliesArea)
            && moved.Cards.Any(card => card.Card == chosen.ObjectId));
        Assert.Equal(ownerEliminated ? nameof(DeckType.RemovedArea) : nameof(DeckType.DiscardPile), move.To.Zone);
        Assert.Equal(reset ? 1 : 0, events.OfType<CardsMoved>().Count(moved => moved.Verb == "Reset"));
        Assert.Equal(reset ? 1 : 0, events.OfType<AreaReordered>().Count());
    }

    private static World Board()
    {
        var world = new World(new Facts(), players: 3);
        for (int player = 0; player < 3; player++)
        {
            world.CreateSeat($"p{player}");
            world.Seats[player].IdentityCard = world.CreateCard("hero", world.Seats[player].Hero);
            world.CreateCard("resource", world.Seats[player].Deck);
        }
        for (int count = 0; count < 3; count++)
            world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "ally" => CardKind.Ally,
            "upgrade" => CardKind.Upgrade,
            "resource" => CardKind.Resource,
            _ => CardKind.Treachery,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "HP" ? 10 : fallback;
    }
}
