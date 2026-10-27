using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local Clea composition; no expansion ability is admitted.</summary>
public sealed class OwnerDeckShuffleTests
{
    [Fact]
    public void YourDeckUsesTheControllerEvenWhenThePhysicalOwnerDiffers()
    {
        var (world, runner, source) = Board("yourDeck", owner: 1);
        Execute(world, runner, source);
        Assert.Same(world.Seats[0].Deck, source.Area);
        Assert.Equal(1, source.Owner);
    }

    [Rule("rr:ownership-and-control")]
    [Fact]
    public void OwnerDeckUsesThePhysicalOwnerInsteadOfTheController()
    {
        // rr:ownership-and-control: "A card's owner is the player whose deck contained the card at the start of setup."
        var (world, runner, source) = Board("ownerDeck", owner: 1);
        var other = Ally(world, "09013", 0);
        int incarnation = source.Incarnation;
        var events = Execute(world, runner, source);
        Assert.Same(world.Seats[1].Deck, source.Area);
        Assert.Equal(1, source.Owner);
        Assert.Equal(incarnation, source.Incarnation);
        Assert.Equal(DeckType.AlliesArea, other.Area.Type);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("yourDeck")]
    [InlineData("ownerDeck")]
    public void SameOwnerDestinationPreservesTheExistingSeededShuffle(string destination)
    {
        var (world, runner, source) = Board(destination, owner: 0);
        var (control, _, expected) = Board("yourDeck", owner: 0);
        World.MoveToTop(expected, control.Seats[0].Deck);
        control.Shuffle(control.Seats[0].Deck);
        Execute(world, runner, source);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
    }

    [Rule("rr:shuffle")]
    [Fact]
    public void MultipleOwnersMoveAllTheirCardsBeforeOneShufflePerDeckInOwnerOrder()
    {
        // rr:shuffle: "Shuffling is the game function of randomizing the contents of a deck."
        // Engine choice: owner order is ascending; existing selector order remains within each owner's group.
        var (world, runner, source) = Board("ownerDeck", owner: 1, selection: "{\"query\":\"allies\"}");
        var (control, _, expected) = Board("ownerDeck", owner: 1, selection: "{\"query\":\"allies\"}");
        var own = Ally(world, "01002", 0);
        var foreign = Ally(world, "01011", 1);
        var expectedOwn = Ally(control, "01002", 0);
        var expectedForeign = Ally(control, "01011", 1);
        World.MoveToTop(expectedOwn, control.Seats[0].Deck);
        control.Shuffle(control.Seats[0].Deck);
        World.MoveToTop(expected, control.Seats[1].Deck);
        World.MoveToTop(expectedForeign, control.Seats[1].Deck);
        control.Shuffle(control.Seats[1].Deck);
        var events = Execute(world, runner, source);
        Assert.Equal(new[] { own.ObjectId, source.ObjectId, foreign.ObjectId },
            events.OfType<CardsMoved>().Where(move => move.Verb == "Shuffle_Into").Select(move => Assert.Single(move.Cards).Card));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void RepeatedOfferQueriesPreserveStateRngPendingWorkAndLaterExecution()
    {
        var (world, runner, source) = Board("ownerDeck", owner: 1);
        var (control, controlRunner, expected) = Board("ownerDeck", owner: 1);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        for (int i = 0; i < 3; i++) Assert.Contains(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(Moves(Execute(control, controlRunner, expected)), Moves(Execute(world, runner, source)));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
    }

    [Rule("rr:interrupt.3")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1, false)]
    public void OptionalDefeatInterruptMovesExactlyOnceAndDeclineUsesOwnerDiscard(int owner, bool accept)
    {
        // Printed 09013: "When Clea is defeated, shuffle her into her owner's deck."
        // rr:interrupt.3: "before that triggering condition resolves."
        var (world, runner, source) = Board("ownerDeck", owner, interrupt: true);
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, world.Facts, new(source, source, 2, "Clea", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.True(prompt.Cancellable);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? Decision.Take(Assert.Single(prompt.Affordances).Id) : Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(accept ? DeckType.PlayerDeck : DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(PlayArea.Of(owner), source.Area.PlayArea);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("yourDiscardPile")]
    [InlineData("ownerHand")]
    [InlineData("OwnerDeck")]
    public void UnsupportedDestinationsAreRejectedDuringLowering(string destination)
    {
        var error = Assert.Throws<AbilityException>(() => Runner(destination));
        Assert.Contains("effect/shuffleInto/deck", error.Message, StringComparison.Ordinal);
        Assert.Contains(destination, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    [InlineData(9, false)]
    [InlineData(9, true)]
    public void NonPlayerOwnersRaiseBeforeAnySelectedCardMovesOrShuffles(int owner, bool invalidFirst)
    {
        var (world, runner, source) = Board("ownerDeck", invalidFirst ? owner : 0, selection: "{\"query\":\"allies\"}");
        Ally(world, "01002", invalidFirst ? 0 : owner);
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var error = Assert.Throws<RulesNotImplementedException>(() => Execute(world, runner, source));
        Assert.Contains("ownerDeck", error.Message, StringComparison.Ordinal);
        Assert.Contains($"owner {owner}", error.Message, StringComparison.Ordinal);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void EncounterDestinationKeepsExistingMovementAndSeededShuffle()
    {
        var (world, runner, source) = Board("encounterDeck", owner: 1);
        var (control, _, expected) = Board("encounterDeck", owner: 1);
        World.MoveToTop(expected, control.AreaOf(DeckType.EncounterDeck));
        control.Shuffle(control.AreaOf(DeckType.EncounterDeck));
        Execute(world, runner, source);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void EmptyOwnerSelectionDoesNotShuffleAnUnspecifiedPlayersDeck()
    {
        var (world, runner, source) = Board("ownerDeck", 0, selection: "{\"query\":\"supports\"}");
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Empty(Execute(world, runner, source));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void OwnerShuffleProjectionRecognizesTheSourceAreaAndPlayerDeck()
    {
        var (_, _, scope) = WreckingProjectionFixture.Create();
        AbilityEffect[] effects = [new AbilityEffect.ShuffleInto(
            new AbilityCardSelection.Bound(AbilityCardBinding.This), AbilityShuffleDestination.OwnerDeck)];
        Assert.True(AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(effects,
            new HashSet<DeckType> { DeckType.PlayerDeck }, scope.Context));
        Assert.True(AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(effects,
            new HashSet<DeckType> { DeckType.SupportsArea }, scope.Context));
        Assert.False(AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(effects,
            new HashSet<DeckType> { DeckType.EncounterDeck }, scope.Context));
    }

    private static IEnumerable<(AreaRef From, AreaRef To, string Trigger, string Verb, int Card, int Position)> Moves(
        IReadOnlyList<GameEvent> events) => events.OfType<CardsMoved>().SelectMany(move =>
            move.Cards.Select(card => (move.From, move.To, move.Trigger, move.Verb, card.Card, card.Index)));

    private static Card Ally(World world, string face, int owner) =>
        world.CreateCard(face, world.CreateArea(DeckType.AlliesArea, cardOwner: owner, playArea: PlayArea.Of(0)));

    private static (World World, AbilityRunner Runner, Card Source) Board(
        string destination, int owner, string selection = "\"this\"", bool interrupt = false)
    {
        var runner = Runner(destination, selection, interrupt);
        var world = ThorActualFixture.Board("06001a", players: 2);
        world.Abilities = runner;
        var source = Ally(world, "09013", owner);
        return (world, runner, source);
    }

    private static AbilityRunner Runner(string destination, string selection = "\"this\"", bool interrupt = false) =>
        new(AbilityCatalog.Parse("""
            {"cards":[{"card":"09013","abilities":[{
              "trigger":{"event":"EVENT","timing":"TIMING","subject":"this"},
              "effect":{"shuffleInto":{"cards":SELECTION,"deck":"DESTINATION"}}}]}]}
            """.Replace("EVENT", interrupt ? "WhenCardDefeated" : "WhenActionTriggered", StringComparison.Ordinal)
            .Replace("TIMING", interrupt ? "Interrupt" : "Action", StringComparison.Ordinal)
            .Replace("SELECTION", selection, StringComparison.Ordinal)
            .Replace("DESTINATION", destination, StringComparison.Ordinal)));

    private static IReadOnlyList<GameEvent> Execute(World world, AbilityRunner runner, Card source)
    {
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var result = runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        return result.Events;
    }
}
