using System.Text.Json;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Research-only complete games using the authored four-deck recipes.</summary>
public sealed class WreckingCrewGameTests(ITestOutputHelper output)
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("wrecking_crew_standard", false, 1u)]
    [InlineData("wrecking_crew_expert", false, 2u)]
    [InlineData("wrecking_crew_extreme", false, 3u)]
    [InlineData("wrecking_crew_custom_abab", false, 4u)]
    [InlineData("wrecking_crew_standard", true, 5u)]
    [InlineData("wrecking_crew_expert", true, 6u)]
    [InlineData("wrecking_crew_extreme", true, 7u)]
    [InlineData("wrecking_crew_custom_abab", true, 8u)]
    public void AuthoredRecipeReachesAnEndingWithExactDecisionReplay(
        string scenario, bool multiplayer, uint seed)
    {
        // Direct research setup exercises the executable book without admitting
        // Wrecking Crew through public discovery or the product factory.
        var game = Deal(scenario, multiplayer, seed);
        var replay = Deal(scenario, multiplayer, seed);
        var policy = new CoreGamePolicy(Cards);
        Assert.Equal(Observe(game), Observe(replay));
        Assert.Equal(4, game.State.EncounterTopology!.Groups.Count);
        for (int decisions = 0; game.Pending is not null; decisions++)
        {
            Assert.True(decisions < 2000,
                $"{scenario}, {multiplayer}, seed {seed}, decision {decisions}: {game.Pending.Label}");
            var prompt = PromptRecord.From(game.Pending);
            var answer = policy.Answer(game);
            Resolution result;
            try
            {
                result = game.Resolve(answer);
            }
            catch (Exception failure)
            {
                throw new InvalidOperationException(
                    $"{scenario}, multiplayer={multiplayer}, seed={seed}, decision={decisions}; "
                    + $"prompt={JsonSerializer.Serialize(prompt)}; "
                    + $"answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.Payments > 0);
        Assert.True(policy.PlayerAttacks > 0);
        output.WriteLine($"{scenario}, multiplayer={multiplayer}, seed={seed}: {game.State.Result}; "
            + $"round={game.Round}, decisions={policy.Answered}, cards={policy.CardsPlayed}, "
            + $"payments={policy.Payments}, attacks={policy.PlayerAttacks}, "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}");
    }

    private static Game Deal(string scenario, bool multiplayer, uint seed)
    {
        string[] heroes = multiplayer ? ["spider_man", "captain_marvel"] : ["spider_man"];
        var plan = ScenarioDealer.Prepare(Setup, new(scenario, heroes), Cards);
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
