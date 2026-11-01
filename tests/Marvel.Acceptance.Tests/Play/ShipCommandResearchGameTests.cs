using System.Text.Json;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Research-only complete games with the withheld Ship Command modular set. Direct setup
/// exercises the executable book without admitting the set through public discovery.
/// </summary>
public sealed class ShipCommandResearchGameTests(ITestOutputHelper output)
{
    private const string Milano = "16142";
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:setup-keyword.1")]
    public void SetupPutsTheScenarioOwnedMilanoInTheFirstPlayersSupports(bool multiplayer)
    {
        // 16142: "Setup. The first player controls the Milano." It stays an encounter card
        // the scenario owns (rr:ownership-and-control).
        var game = Deal("rhino", multiplayer, 1);
        var milano = Assert.Single(game.State.Cards, card => card.FaceId == Milano);

        Assert.Equal(DeckType.SupportsArea, milano.Area.Type);
        Assert.Equal(PlayArea.Of(game.State.FirstPlayer), milano.Area.PlayArea);
        Assert.Equal(World.Scenario, milano.Owner);
        Assert.Equal(game.State.FirstPlayer, CardControl.ControllerOf(game.State, milano));
    }

    [Theory]
    [InlineData("rhino", false, 1u)]
    [InlineData("klaw", false, 2u)]
    [InlineData("ultron", false, 3u)]
    [InlineData("rhino", true, 4u)]
    [InlineData("klaw", true, 5u)]
    public void ShipCommandGamesReachAnEndingWithExactDecisionReplay(string scenario, bool multiplayer, uint seed)
    {
        var game = Deal(scenario, multiplayer, seed);
        var replay = Deal(scenario, multiplayer, seed);
        var policy = new CoreGamePolicy(Cards);
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}, seed={seed}: {prompt.Label}");
            var answer = policy.Answer(game);
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}, multiplayer={multiplayer}, seed={seed}, "
                    + $"decision={policy.Answered}; prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}; "
                    + $"answer={JsonSerializer.Serialize(answer)}", failure);
            }
            Assert.Equal(Observe(game, result), Observe(replay, replay.Resolve(answer)));
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Null(replay.Pending);
        output.WriteLine($"{scenario}, multiplayer={multiplayer}, seed={seed}: {game.State.Result}; "
            + $"round={game.Round}, decisions={policy.Answered}, rngWords={game.State.Random.Generator.WordsConsumed}");
    }

    private static Game Deal(string scenario, bool multiplayer, uint seed)
    {
        string[] heroes = multiplayer ? ["she_hulk", "groot"] : ["she_hulk"];
        var plan = ScenarioDealer.Prepare(Setup, new(scenario, heroes, ["ship_command"]), Cards);
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints,
            [.. heroes.Select(hero => Setup.Hero(hero).Name)], seed, abilities,
            new(Expert: Setup.Campaign(scenario).Expert, EncounterGroups: plan.EncounterGroups));
        return Game.Begin(world, Cards, abilities);
    }

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(),
        RngWords = game.State.Random.Generator.WordsConsumed,
        game.State.Result,
        game.Phase,
        game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events,
        result?.Information,
    });
}
