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

/// <summary>Research pack integration through real setup, play and deterministic replay.</summary>
public sealed class CaptainAmericaGameTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("rhino", false, 560u)]
    [InlineData("rhino_expert", false, 561u)]
    [InlineData("klaw", false, 562u)]
    [InlineData("klaw_expert", false, 563u)]
    [InlineData("ultron", false, 564u)]
    [InlineData("ultron_expert", false, 565u)]
    [InlineData("rhino", true, 566u)]
    [InlineData("crossbones", true, 567u)]
    public void StarterDeckReachesAnEndingWithIdenticalReplayedDecisions(
        string scenario, bool multiplayer, uint seed)
    {
        var game = Deal(scenario, multiplayer, seed);
        var replay = Deal(scenario, multiplayer, seed);
        var policy = new CoreGamePolicy(Cards);
        Assert.Equal(Observe(game), Observe(replay));
        for (int decisions = 0; game.Pending is not null; decisions++)
        {
            Assert.True(decisions < 2000, $"{scenario}: {game.Pending.Label}");
            var answer = policy.Answer(game);
            var result = game.Resolve(answer);
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.Payments > 0);
        Assert.True(policy.PlayerAttacks > 0);
        Assert.Null(replay.Pending);
    }

    private static Game Deal(string scenario, bool multiplayer, uint seed)
    {
        string[] heroes = multiplayer ? ["captain_america", "spider_man"] : ["captain_america"];
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.Deal(Cards,
            Blueprints.From(Dealer.DealOrder(Setup, scenario, heroes), Cards),
            [.. heroes.Select(hero => Setup.Hero(hero).Name)], seed, abilities,
            expert: Setup.Campaign(scenario).Expert);
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
