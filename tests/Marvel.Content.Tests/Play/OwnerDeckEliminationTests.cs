using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic owner-routing probes after actual elimination; no pack admission.</summary>
public sealed class OwnerDeckEliminationTests
{
    [Fact]
    public void OwnerShuffleProjectionIncludesRemovalButLegacyDestinationsDoNot()
    {
        var (_, _, scope) = WreckingProjectionFixture.Create();
        var selection = new AbilityCardSelection.Bound(AbilityCardBinding.This);
        var removed = new HashSet<DeckType> { DeckType.RemovedArea };
        foreach (var destination in Enum.GetValues<AbilityShuffleDestination>())
        {
            AbilityEffect[] effects = [new AbilityEffect.ShuffleInto(selection, destination)];
            Assert.Equal(destination == AbilityShuffleDestination.OwnerDeck,
                AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(effects, removed, scope.Context));
        }
    }

    [Rule("rr:ownership-and-control.7.2")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void MixedOwnersRemoveEliminatedCardsAndShuffleOnlyTheActiveDeck(int eliminated)
    {
        // rr:ownership-and-control.7.2: "removed from the game if that player is no longer in the game".
        // Engine choice: ascending owner order, retaining selector order within each owner group.
        var (world, runner, cards) = Board(eliminated);
        var (control, _, expected) = Board(eliminated);
        var removed = world.AreaOf(DeckType.RemovedArea);
        int[] removedBefore = [.. removed.Cards.Select(card => card.ObjectId)];
        Assert.NotEmpty(removedBefore);
        Assert.Empty(world.Seats[eliminated].Deck.Cards);
        Assert.Equal(12, world.Seats[1 - eliminated].Deck.Cards.Count);

        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++)
            Assert.Contains(runner.Actions(world, 2), action => action.Card == cards[0].ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);

        for (int owner = 0; owner < 2; owner++)
        {
            var destination = owner == eliminated
                ? control.AreaOf(DeckType.RemovedArea) : control.Seats[owner].Deck;
            foreach (var card in expected.Where(card => card.Owner == owner))
                World.MoveToTop(card, destination);
            if (owner != eliminated) control.Shuffle(destination);
        }
        var action = Assert.Single(runner.Actions(world, 2), item => item.Card == cards[0].ObjectId);
        var result = runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);

        var moves = result.Events.OfType<CardsMoved>().ToArray();
        Assert.Equal(new[] { cards[1].ObjectId, cards[0].ObjectId, cards[2].ObjectId },
            moves.Select(move => Assert.Single(move.Cards).Card));
        foreach (var card in cards)
        {
            var move = Assert.Single(moves, move => move.Cards.Any(landing => landing.Card == card.ObjectId));
            Assert.Equal("AlliesArea", move.From.Zone);
            Assert.Equal(card.Owner == eliminated ? "RemovedArea" : "PlayerDeck", move.To.Zone);
            Assert.Equal("Shuffle_Into", move.Verb);
        }
        Assert.Equal(removedBefore.Concat(cards.Where(card => card.Owner == eliminated).Select(card => card.ObjectId)),
            removed.Cards.Select(card => card.ObjectId));
        Assert.Empty(world.Seats[eliminated].Deck.Cards);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void AnInvalidOwnerPreventsRemovalOfAnEliminatedOwnersCards(int invalidOwner)
    {
        var (world, runner, cards) = Board(eliminated: 1);
        Ally(world, "01002", invalidOwner);
        var action = Assert.Single(runner.Actions(world, 2), item => item.Card == cards[0].ObjectId);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteAction(world, new PlayerAction(action, [], [])));
        Assert.Contains($"owner {invalidOwner}", error.Message, StringComparison.Ordinal);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.All(cards, card => Assert.Equal(DeckType.AlliesArea, card.Area.Type));
    }

    private static (World World, AbilityRunner Runner, Card[] Cards) Board(int eliminated)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"09013","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"shuffleInto":{"cards":{"query":"allies"},"deck":"ownerDeck"}}}]}]}
            """));
        var world = ThorActualFixture.Board("06001a", players: 3);
        world.Abilities = runner;
        Card[] cards = [Ally(world, "09013", 1), Ally(world, "01002", 0), Ally(world, "01011", 1)];
        Elimination.Eliminate(world, world.Facts, eliminated, "OwnerRoutingProbe", []);
        Assert.True(world.Seats[eliminated].Eliminated);
        Assert.All(cards, card => Assert.Equal(DeckType.AlliesArea, card.Area.Type));
        return (world, runner, cards);
    }

    private static Card Ally(World world, string face, int owner) =>
        world.CreateCard(face, world.CreateArea(
            DeckType.AlliesArea, cardOwner: owner, playArea: PlayArea.Of(2)));
}
