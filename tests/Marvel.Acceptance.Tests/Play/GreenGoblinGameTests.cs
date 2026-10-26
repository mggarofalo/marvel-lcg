using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Complete seeded games dealt by the admitted production factory.</summary>
public sealed class GreenGoblinGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("mutagen_formula", "captain_america", 11u, Outcome.PlayersLose, "02014,02015")]
    [InlineData("mutagen_formula_expert", "captain_america", 23u, Outcome.PlayersLose, "02015")]
    [InlineData("risky_business", "spider_man", 1u, Outcome.PlayersLose, "02001a,02001b")]
    [InlineData("risky_business_expert", "spider_man", 2u, Outcome.PlayersLose, "02002a,02002b")]
    [InlineData("mutagen_formula", "spider_man", 3u, Outcome.PlayersLose, "02014,02015")]
    [InlineData("mutagen_formula_expert", "spider_man", 4u, Outcome.PlayersLose, "02015")]
    [InlineData("risky_business", "captain_america", 3u, Outcome.PlayersLose, "02001a,02001b,02002a,02002b")]
    [InlineData("risky_business_expert", "captain_america", 10u, Outcome.PlayersLose, "02002a,02002b,02003a,02003b")]
    [InlineData("risky_business", "captain_america", 8u, Outcome.VillainWins, "02001a,02001b")]
    [InlineData("risky_business_expert", "captain_america", 12u, Outcome.VillainWins, "02002a,02002b")]
    public void ProductionSetupReachesAnEndingWithExactDecisionReplay(
        string scenario, string hero, uint seed, Outcome outcome, string stages)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(scenario, [hero], null, seed);
        var game = factory.Create(specification).Game;
        var replay = factory.Create(specification).Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is not null)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}, hero={hero}, seed={seed}: {game.Pending.Label}");
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
                    $"{scenario}, hero={hero}, seed={seed}, decision={policy.Answered}; "
                    + $"prompt={JsonSerializer.Serialize(prompt)}; answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        Assert.Equal(outcome, game.State.Result);
        Assert.Equal(stages.Split(','), policy.VillainStages.Order());
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.Payments > 0);
        Assert.True(policy.PlayerAttacks > 0);
        output.WriteLine($"{scenario}, hero={hero}, seed={seed}: {game.State.Result}; round={game.Round}, "
            + $"decisions={policy.Answered}, cards={policy.CardsPlayed}, payments={policy.Payments}, "
            + $"attacks={policy.PlayerAttacks}, stages={string.Join(',', policy.VillainStages.Order())}, "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}");
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
