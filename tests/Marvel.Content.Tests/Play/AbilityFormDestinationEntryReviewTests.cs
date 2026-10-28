using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic admission-boundary probes; these do not author or admit Ant-Man cards.
public sealed class AbilityFormDestinationEntryReviewTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("setup")]
    [InlineData("reveal")]
    [InlineData("boost")]
    public void OrderedEntryRejectsAComposedDestinationBeforeSchedulingItsFlip(string path)
    {
        // Engine choice: exact destinations are admitted only as standalone effects.
        // A one-child simultaneous wrapper exposes entry paths that bypass admission.
        const string change = """{"changeForm":{"player":"you","to":{"face":"12001c"}}}""";
        var runner = Runner(path, """{"and":[CHANGE]}""".Replace("CHANGE", change, StringComparison.Ordinal));
        var (world, source) = Board(runner, "01006");
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();

        var error = Record.Exception(() => Enter(path, runner, world, source));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Contains("composed", Assert.IsType<RulesNotImplementedException>(error).Message);

    }

    [Theory]
    [InlineData("setup")]
    [InlineData("reveal")]
    [InlineData("boost")]
    public void OrderedEntryAcceptsAStandaloneDestination(string path)
    {
        const string change = """{"changeForm":{"player":"you","to":{"face":"12001c"}}}""";
        var controlRunner = Runner(path, change);
        var (control, controlSource) = Board(controlRunner, "01006");
        Enter(path, controlRunner, control, controlSource);
        Assert.Equal("12001c", Assert.Single(control.Agenda.Outstanding)
            .Require<IdentityFormChangeOperation>().Destination);
    }

    [Fact]
    public void DirectScenarioWindowPreparationRejectsTheSameCompositeAsDiscovery()
    {
        const string effect = """{"and":[{"changeForm":{"player":"firstPlayer","to":{"face":"12001c"}}}]}""";
        var runner = Runner("window", effect);
        var (world, source) = Board(runner, "01114");
        // Villain-phase beginning is a scenario occurrence with no resolving seat.
        var occurrence = new Occurrence(1, [VillainPhaseBeginning.Begins]);
        var address = new PendingAbility(source.ObjectId, AbilityType.ForcedResponse, World.Scenario);
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();

        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Waiting(world, occurrence, WindowKind.Response));
        var error = Record.Exception(() =>
            runner.PrepareWindow(world, occurrence, address, Decision.Take(source.ObjectId)));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Contains("composed", Assert.IsType<RulesNotImplementedException>(error).Message);
    }

    [Fact]
    public void ScenarioWindowPreparationAcceptsAStandaloneDestination()
    {
        var runner = Runner("window", """{"changeForm":{"player":"firstPlayer","to":{"face":"12001c"}}}""");
        var (world, source) = Board(runner, "01114");
        var occurrence = new Occurrence(1, [VillainPhaseBeginning.Begins]);
        var address = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));

        Assert.NotNull(runner.PrepareWindow(world, occurrence, address, Decision.Take(source.ObjectId)));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void SetupRejectsDelayedDestinationBeforeRegisteringFutureWork()
    {
        const string effect = """{"afterActivation":{"effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}}}""";
        var runner = Runner("setup", effect);
        var (world, source) = Board(runner, "01006");
        var enemy = world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        world.Activation = new EnemyActivation(enemy.ObjectId, 0, Attacking: true, Id: 41);
        Assert.Null(world.AbilityRuntime);
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();

        var error = Record.Exception(() => runner.ExecuteSetup(world, source));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Null(world.AbilityRuntime);
        Assert.Contains("composed", Assert.IsType<RulesNotImplementedException>(error).Message);
    }

    [Fact]
    public void SetupCanRegisterASupportedDelayedEffectDuringAnActivation()
    {
        var runner = Runner("setup", """{"afterActivation":{"effect":{"draw":{"player":"you","count":1}}}}""");
        var (world, source) = Board(runner, "01006");
        var enemy = world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        world.Activation = new EnemyActivation(enemy.ObjectId, 0, Attacking: true, Id: 41);

        runner.ExecuteSetup(world, source);

        Assert.NotNull(world.AbilityRuntime);
    }

    private static void Enter(string path, AbilityRunner runner, World world, Card source)
    {
        if (path == "setup") runner.ExecuteSetup(world, source);
        else if (path == "reveal") runner.ExecuteReveal(world, source, 0);
        else runner.ExecuteBoost(world, source, 0);
    }

    private static AbilityRunner Runner(string path, string effect)
    {
        string trigger = path switch
        {
            "setup" => """{"timing":"Setup"}""",
            "window" => """{"event":"WhenVillainPhaseBegins","timing":"ForcedResponse","subject":"game"}""",
            _ => $$$$"""{"event":"WhenCardRevealed","timing":"{{{{(path == "boost" ? "Boost" : "WhenRevealed")}}}}","subject":"this"}""",
        };
        string card = path == "window" ? "01114" : "01006";
        return new AbilityRunner(AbilityCatalog.Parse($$"""
            {"cards":[{"card":"{{card}}","abilities":[{"trigger":{{trigger}},"effect":{{effect}}}]}]}
            """));
    }

    private static (World World, Card Source) Board(AbilityRunner runner, string face)
    {
        var world = new World(Cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        for (int index = 0; index < 4; index++) world.CreateCard("01087", seat.Deck);
        var source = world.CreateCard(face, face == "01114"
            ? world.AreaOf(DeckType.VillainArea)
            : world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        return (world, source);
    }
}
