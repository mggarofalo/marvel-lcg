using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class GreenGoblinMultiplayerTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("risky_business", 7u)]
    [InlineData("risky_business_expert", 7u)]
    [InlineData("mutagen_formula", 8u)]
    [InlineData("mutagen_formula_expert", 8u)]
    public void TwoPublicStartersCompleteEveryModeWithExactDecisionReplay(string scenario, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(scenario, ["spider_man", "captain_marvel"], null, seed);
        var game = factory.Create(specification).Game;
        var replay = factory.Create(specification).Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var actingPlayers = new HashSet<int>();
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}: {prompt.Label}");
            if (prompt.Asking == Marvel.Rules.Prompts.Question.TurnOption)
                actingPlayers.Add(prompt.Player);
            var answer = policy.Answer(game);
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}, seed={seed}, decision={policy.Answered}, "
                    + $"prompt={prompt.Label}, answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        Assert.Equal([0, 1], actingPlayers.Order());
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.Payments > 0);
        Assert.True(policy.PlayerAttacks > 0);
        output.WriteLine($"{scenario}, seed={seed}: {game.State.Result}, round={game.Round}, "
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
