using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic capability fixtures and actual authored Risky Business completion.
public sealed class MainSchemeCompletionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-completed-abilities.1")]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    public void NestedCompletionChoiceFinishesBeforeAdvanceOrFinalLoss(bool final)
    {
        // "Forced Interrupt: When this scheme is completed..." resolves before
        // completion; "the villain wins the game" only after the final completion.
        var runner = Runner("""
            {"seq":[
              {"choose":{"options":[
                {"placeCounters":{"card":"this","counter":"observed","count":1}},
                {"placeCounters":{"card":"this","counter":"observed","count":2}}]}},
              {"placeThreat":{"scheme":"this","amount":1}},
              {"placeCounters":{"card":{"titled":"Criminal Enterprise"},"counter":"infamy","count":{"perPlayer":1}}}
            ]}
            """);
        var (world, scheme, environment) = Board(runner, 2, final);
        var events = new List<GameEvent>();
        Threat.Place(world, Cards, runner, scheme, 14, "test", events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("is_completed"));
        Threat.Completed(world, Cards, runner, scheme, events);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(1), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, environment.Tokens["c_infamy"]);
        Assert.Single(events.OfType<FieldSet>(), e => e.Field == "is_completed");
        Assert.Equal(final ? Outcome.VillainWins : Outcome.Unfinished, world.Result);
        Assert.Equal(final ? DeckType.MainSchemesArea : DeckType.RemovedArea, scheme.Area.Type);
        if (!final) Assert.Equal(2, events.OfType<FieldSet>().Single(e => e.Field == "c_observed").To);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(1, 8, true)]
    [InlineData(2, 8, true)]
    [InlineData(2, 2, true)]
    [InlineData(2, 8, false)]
    [InlineData(4, 8, true)]
    [Rule("rr:when-completed-abilities.1")]
    [Rule("rr:player-deck.3")]
    public void HostileTakeoverAddsPerPlayerInfamyThenDiscardsTheResultingCount(int players, int deckSize, bool environmentPresent)
    {
        // 02004b: "Place 1[per_hero] infamy counters ... Then discard 1 card
        // from each player's deck for each infamy counter". Player-deck.3 stops
        // discarding when the deck empties, after shuffling and dealing a card.
        // Present Criminal Enterprise receives this positive placement in full;
        // the guard makes the dependent instruction fail with its antecedent.
        var runner = AuthoredCards.Runner();
        var (world, scheme, environment) = Board(runner, players, true);
        environment.PlaceTokens("c_infamy", 2);
        if (!environmentPresent) World.MoveToTop(environment, world.AreaOf(DeckType.RemovedArea));
        for (int player = 0; player < players; player++)
        {
            var deck = world.AreaOf(DeckType.PlayerDeck, PlayArea.Of(player), cardOwner: player);
            for (int i = 0; i < deckSize; i++) world.CreateCard("01089", deck);
        }
        world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        Threat.Place(world, Cards, runner, scheme, 7 * players, "test", events);
        FinishChoices(world, runner, events);
        Assert.Equal(environmentPresent ? 2 + players : 0, environment.Tokens.GetValueOrDefault("c_infamy"));
        int discarded = environmentPresent ? 2 + players : 0;
        for (int player = 0; player < players; player++)
            AssertDeckResult(world, player, deckSize, discarded);
        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    private static void AssertDeckResult(World world, int player, int deckSize, int discarded)
    {
        var deck = world.AreaOf(DeckType.PlayerDeck, PlayArea.Of(player), cardOwner: player);
        Assert.Equal(deckSize > discarded ? deckSize - discarded : deckSize, deck.Cards.Count);
        Assert.Equal(deckSize > discarded ? discarded : 0,
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards.Count);
        Assert.Equal(deckSize > discarded ? 0 : 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player), cardOwner: player).Cards.Count);
    }

    [Fact]
    public void UnimplementedPrintedCompletionRaisesBeforeCompletionOrAdvance()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}"));
        var (world, scheme, _) = Board(runner, 1, false);
        var events = new List<GameEvent>();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Threat.Place(world, Cards, runner, scheme, 7, "test", events));
        Assert.Contains("When Completed", error.Message);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("is_completed"));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"02004b","abilities":[{
          "trigger":{"event":"WhenMainSchemeCompleted","timing":"WhenCompleted","subject":"this"},
          "effect":EFFECT}]},
          {"card":"01137a","abilities":[]},{"card":"01137b","abilities":[]}]}
        """.Replace("EFFECT", effect, StringComparison.Ordinal)));

    private static (World, Card, Card) Board(AbilityRunner runner, int players, bool final)
    {
        var world = Hero("01001a", players);
        world.Abilities = runner;
        var scheme = world.CreateCard("02004b", world.AreaOf(DeckType.MainSchemesArea));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        if (!final) world.CreateCard("01137a,01137b", world.AreaOf(DeckType.MainSchemesDeck));
        return (world, scheme, environment);
    }

    private static void FinishChoices(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Assert.Equal(Question.Order, prompt.Asking);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(prompt.Affordances[0].Id, world.PlayerOrder.ToArray(), []), events);
        }
        Assert.Fail("Completion did not settle");
    }
}
