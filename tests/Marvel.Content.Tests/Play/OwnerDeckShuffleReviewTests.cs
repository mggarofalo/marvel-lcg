using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent synthetic probes of owner-relative shuffle routing.</summary>
public sealed class OwnerDeckShuffleReviewTests
{
    [Fact]
    public void AnEmptyOwnersDeckReceivesTheExactCardWithoutAResetOrRandomDraw()
    {
        var (world, runner, source) = Board();
        foreach (var card in world.Seats[1].Deck.Cards.ToArray())
            World.MoveToTop(card, world.Seats[1].Hand);
        long words = world.Random.Generator.WordsConsumed;
        var events = Execute(world, runner, source);
        Assert.Same(source, Assert.Single(world.Seats[1].Deck.Cards));
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Single(events.OfType<CardsMoved>());
        Assert.DoesNotContain(events.OfType<CardsMoved>(), move => move.Verb != "Shuffle_Into");
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:ownership-and-control.7.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADepartingForeignControlledCardUsesItsOwnersParticipationState(bool eliminated)
    {
        // rr:ownership-and-control.7.2: "removed from the game if that player is no longer in the game".
        var (world, runner, source) = Board();
        if (eliminated)
        {
            Elimination.Eliminate(world, world.Facts, 1, "Review", []);
            Assert.Equal(DeckType.RemovedArea, source.Area.Type);
            // Introduce a synthetic late source to exercise routing to an absent owner.
            source = world.CreateCard("09013", world.CreateArea(
                DeckType.AlliesArea, cardOwner: 1, playArea: PlayArea.Of(0)));
        }
        Assert.Equal(eliminated, world.Seats[1].Eliminated);
        Assert.Equal(DeckType.AlliesArea, source.Area.Type);
        Assert.Equal(eliminated ? 0 : 12, world.Seats[1].Deck.Cards.Count);
        long words = world.Random.Generator.WordsConsumed;
        var events = Execute(world, runner, source);
        if (eliminated) Assert.Equal(words, world.Random.Generator.WordsConsumed);
        else Assert.True(world.Random.Generator.WordsConsumed > words);
        var move = Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.Equal(source.ObjectId, Assert.Single(move.Cards).Card);
        Assert.Equal("AlliesArea", move.From.Zone);
        Assert.Equal(eliminated ? "RemovedArea" : "PlayerDeck", move.To.Zone);
        Assert.Equal(eliminated ? DeckType.RemovedArea : DeckType.PlayerDeck, source.Area.Type);
        Assert.Equal(eliminated ? 0 : 13, world.Seats[1].Deck.Cards.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnAmbiguousDeckReadAfterOwnerShuffleRaisesBeforeExhaustingTheSource(bool nested)
    {
        const string shuffle = """{"shuffleInto":{"cards":"this","deck":"ownerDeck"}}""";
        string first = nested ? $$"""{"seq":[{{shuffle}}]}""" : shuffle;
        string effect = $$$"""
            {"seq":[{{{first}}},
              {"heal":{"card":{"cardsIn":{"area":"yourDeck","title":"Clea"}},"amount":1}}]}
            """;
        var (world, runner, source) = Board(effect, exhaust: true, owner: 0);
        var other = world.CreateCard("09013", world.Seats[0].Deck);
        other.TakeDamage(1);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        Assert.True(source.Ready);
    }

    [Fact]
    public void AnIndependentDeckReadAfterNestedOwnerShuffleRemainsExecutable()
    {
        const string effect = """
            {"seq":[{"seq":[{"shuffleInto":{"cards":"this","deck":"ownerDeck"}}]},
              {"removeFromGame":{"cardsIn":{"area":"encounterDeck","title":"Clea"}}}]}
            """;
        var (world, runner, source) = Board(effect, exhaust: true);
        var independent = world.CreateCard("09013", world.AreaOf(DeckType.EncounterDeck));
        var events = Execute(world, runner, source);
        Assert.Same(world.Seats[1].Deck, source.Area);
        Assert.Equal(DeckType.RemovedArea, independent.Area.Type);
        Assert.Single(events.OfType<FieldSet>(), change => change.Verb == "Exhaust");
        Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Any(card => card.Card == independent.ObjectId));
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(
        string effect = """{"shuffleInto":{"cards":"this","deck":"ownerDeck"}}""", bool exhaust = false, int owner = 1)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse($$"""
            {"cards":[{"card":"09013","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              {{(exhaust ? "\"cost\":{\"exhaust\":\"this\"}," : "")}}
              "effect":{{effect}}}]}]}
            """));
        var world = ThorActualFixture.Board("06001a", players: 2);
        world.Abilities = runner;
        var source = world.CreateCard("09013", world.CreateArea(
            DeckType.AlliesArea, cardOwner: owner, playArea: PlayArea.Of(0)));
        return (world, runner, source);
    }

    private static IReadOnlyList<GameEvent> Execute(World world, AbilityRunner runner, Card source)
    {
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var result = runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        return result.Events;
    }
}
