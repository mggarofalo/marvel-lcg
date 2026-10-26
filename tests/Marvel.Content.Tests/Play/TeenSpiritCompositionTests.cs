using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TeenSpiritCompositionTests
{
    [Theory]
    [InlineData("01088")]
    [InlineData("01089")]
    public void DependentDiscardLookupIsRefusedBeforeCostsOrHiddenDeckEffects(string hiddenTop)
    {
        // Synthetic composition: the first effect can add a second Energy to the
        // discard pile. Admission cannot know its future singular lookup result
        // without inspecting hidden deck contents, so it refuses the composition.
        const string program = """
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"seq":[
                {"discardUntil":{"from":"yourDeck","set":"ms_marvel","then":"addToHand"}},
                {"placeCounters":{"card":LOOKUP,"counter":"review","count":1}}
              ]}
            }]}]}
            """;
        var runner = new AbilityRunner(AbilityCatalog.Parse(program.Replace("LOOKUP",
            """{"cardsIn":{"area":"yourDiscardPile","title":"Energy"}}""", StringComparison.Ordinal)));
        var safe = new AbilityRunner(AbilityCatalog.Parse(program.Replace("LOOKUP", "\"this\"", StringComparison.Ordinal)));
        Card? source = null;
        Card? recovered = null;
        var (game, world) = Playing(board =>
        {
            var seat = board.Seats[0];
            var removed = board.AreaOf(DeckType.RemovedArea, PlayArea.Of(0));
            foreach (var card in seat.Deck.Cards.ToArray())
                World.MoveToTop(card, removed);
            var discard = board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
            board.CreateCard("01088", discard);
            board.CreateCard("01089", seat.Deck);
            recovered = board.CreateCard("05003", seat.Deck);
            board.CreateCard(hiddenTop, seat.Deck);
            source = board.CreateCard("01007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        });
        foreach (var card in world.Seats[0].Hand.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(0)));
        Assert.Equal(3, world.Seats[0].Deck.Cards.Count);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var prompt = game.Pending;
        bool busy = world.Agenda.IsBusy;
        var agenda = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        long words = world.Random.Generator.WordsConsumed;
        var allowed = Assert.Single(safe.Actions(world, 0));
        for (int query = 0; query < 2; query++) Assert.Empty(runner.Actions(world, 0));
        Assert.Equal(before, world.Digest().Canonical());
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(forged, [], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Same(prompt, game.Pending);
        Assert.Equal(busy, world.Agenda.IsBusy);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.True(source.Ready);

        safe.ExecuteAction(world, new PlayerAction(allowed, [], []));
        Assert.False(source.Ready);
        Assert.Equal(1, source.Tokens["c_review"]);
        Assert.Same(world.Seats[0].Hand, recovered!.Area);
        Assert.Single(world.Seats[0].Deck.Cards);
    }
}
