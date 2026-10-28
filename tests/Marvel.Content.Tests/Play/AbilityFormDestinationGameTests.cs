using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic actions use the printed identity faces without admitting the pack.
public sealed class AbilityFormDestinationGameTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("12001a", "12001b")]
    [InlineData("12001a", "12001c")]
    [InlineData("12001b", "12001a")]
    [InlineData("12001b", "12001c")]
    [InlineData("12001c", "12001a")]
    [InlineData("12001c", "12001b")]
    [Rule("rr:form-change-form.3")]
    public void ExactAuthoredFaceChangesThroughGameWithoutSpendingVoluntaryChange(string from, string to)
    {
        // rr:form-change-form.3: a card ability does not count against the voluntary change.
        var runner = Runner($$$$"""{"changeForm":{"player":"you","to":{"face":"{{{{to}}}}"}}}""");
        var (world, source) = Board(runner, from);
        var game = Game.Begin(world, Cards, runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == source.ObjectId);
        var result = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(to, world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(0, world.Seats[0].FormChangedInRound);
        Assert.Single(result.Events.OfType<CardsFlipped>());
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001a", "12001c")]
    [InlineData("12001c", "12001a")]
    public void OtherHeroDestinationRunsThroughGame(string from, string to)
    {
        var runner = Runner("""{"changeForm":{"player":"you","to":"other-hero"}}""");
        var (world, source) = Board(runner, from);
        var game = Game.Begin(world, Cards, runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == source.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(to, world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(0, world.Seats[0].FormChangedInRound);
    }

    [Theory]
    [InlineData("""{"changeForm":{"player":"you","to":{"face":"01001a"}}}""")]
    [InlineData("""{"seq":[{"changeForm":{"player":"firstPlayer","to":"other-hero"}},{"draw":{"player":"you","count":1}}]}""")]
    [InlineData("""{"seq":[{"changeForm":{"player":"you","to":{"face":"12001c"}}},{"draw":{"player":"you","count":1}}]}""")]
    [InlineData("""{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"if":{"test":{"inForm":{"player":"you","form":"hero"}},"then":{"draw":{"player":"you","count":1}}}}]}""")]
    [InlineData("""{"afterActivation":{"effect":{"changeForm":{"player":"you","to":"other-hero"}}}}""")]
    public void InvalidOrUnprojectedDestinationFailsBeforeMutatingTheQuery(string effect)
    {
        var runner = Runner(effect);
        var (world, _) = Board(runner, "12001a");
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
    }

    [Fact]
    public void DestinationQueriesPreserveTheNextDecisionComparedWithAnUnprobedGame()
    {
        const string effect = """{"changeForm":{"player":"you","to":"other-hero"}}""";
        var runner = Runner(effect);
        var controlRunner = Runner(effect);
        var (world, source) = Board(runner, "12001a");
        var (control, controlSource) = Board(controlRunner, "12001a");
        var game = Game.Begin(world, Cards, runner);
        var controlGame = Game.Begin(control, Cards, controlRunner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ResolveMulligans).Id));
        controlGame.Resolve(Decision.Take(Assert.Single(controlGame.Pending!.Affordances,
            a => a.Verb == Game.ResolveMulligans).Id));
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        var actionId = Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == source.ObjectId).Id;
        var controlId = Assert.Single(controlGame.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == controlSource.ObjectId).Id;
        var result = game.Resolve(Decision.Take(actionId));
        var controlResult = controlGame.Resolve(Decision.Take(controlId));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(controlResult.Events.Select(item => item.GetType().Name),
            result.Events.Select(item => item.GetType().Name));
        Assert.Equal(controlGame.Pending!.Asking, game.Pending!.Asking);
        Assert.Equal(control.Agenda.Outstanding, world.Agenda.Outstanding);
    }

    [Fact]
    public void ScenarioWindowRejectsComposedDestinationBeforeCreatingPendingWork()
    {
        const string effect = """{"then":{"effect":{"changeForm":{"player":"you","to":"other-hero"}},"then":{"draw":{"player":"you","count":1}}}}""";
        var runner = ScenarioRunner(effect);
        var (world, _) = Board(runner, "12001a");
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var outstanding = world.Agenda.Outstanding.ToArray();
        var occurrence = new Occurrence(1, [Steps.FormChanged], Player: 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        var control = ScenarioRunner("""{"draw":{"player":"you","count":1}}""");
        Assert.Single(control.Waiting(world, occurrence, WindowKind.Response));
    }

    private static AbilityRunner ScenarioRunner(string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"01114","abilities":[{
          "trigger":{"event":"WhenFormChanged","timing":"ForcedResponse","subject":"game"},
          "effect":{{effect}}
        }]}]}
        """));

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"01006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "cost":{"exhaust":"this"},"effect":{{effect}}
        }]}]}
        """));

    private static (World World, Card Source) Board(AbilityRunner runner, string face)
    {
        var world = new World(Cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        return (world, source);
    }
}
