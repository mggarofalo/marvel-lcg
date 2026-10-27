using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic ownership and nested-host controls for elimination.</summary>
public sealed class EliminationOwnedIndependentReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:player-elimination.step.4")]
    [Rule("rr:attach-to.1")]
    public void ForeignControlledOwnedHostDiscardsNestedCardsOnce(bool transferred)
    {
        // Step 4 says "each card owned by the eliminated player". Attach-to.1:
        // "if the game element an attachment is attached to leaves play,
        // the attachment is discarded." Control does not replace ownership.
        var world = new World(new Facts(), players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        }
        world.CreateCard("upgrade", world.Seats[1].Deck);
        var minion = world.CreateCard("minion", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(transferred ? 0 : 1), cardOwner: -1));
        var owned = world.CreateCard("upgrade", world.CreateArea(
            DeckType.UpgradesArea, playArea: minion.Area.PlayArea,
            host: minion.ObjectId, cardOwner: 0));
        var foreign = world.CreateCard("upgrade", world.CreateArea(
            DeckType.UpgradesArea, playArea: minion.Area.PlayArea,
            host: owned.ObjectId, cardOwner: 1));
        var nested = world.CreateCard("upgrade", world.CreateArea(
            DeckType.UpgradesArea, playArea: minion.Area.PlayArea,
            host: foreign.ObjectId, cardOwner: -1));
        var retained = world.CreateCard("upgrade", world.CreateArea(
            DeckType.UpgradesArea, playArea: minion.Area.PlayArea,
            host: minion.ObjectId, cardOwner: 1));
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();

        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), 0);

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.All(new[] { owned, foreign, nested }, card => Assert.Contains(card.ObjectId, layout.Leaving));
        Assert.DoesNotContain(retained.ObjectId, layout.Leaving);
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 0, "review", events);

        Assert.Equal(DeckType.RemovedArea, owned.Area.Type);
        Assert.Equal(DeckType.DiscardPile, foreign.Area.Type);
        Assert.Equal(PlayArea.Of(1), foreign.Area.PlayArea);
        Assert.Equal(DeckType.EncounterDiscardPile, nested.Area.Type);
        Assert.Equal(PlayArea.Of(1), minion.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), retained.Area.PlayArea);
        Assert.Equal(minion.ObjectId, retained.Area.Host);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        foreach (var card in new[] { owned, foreign, nested })
        {
            Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
                && move.Cards.Any(landing => landing.Card == card.ObjectId));
            Assert.Single(events.OfType<CardDetached>(), move => move.Card == card.ObjectId);
        }
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string id) => id == "hero" ? CardKind.Hero
            : id == "minion" ? CardKind.Minion : CardKind.Upgrade;
        public IReadOnlyList<string> Traits(string id) => [];
        public IReadOnlyDictionary<string, string> Attributes(string id) => new Dictionary<string, string>();
        public long PrintedValue(string id, string attribute, int players, long fallback = 0) => fallback;
    }
}
