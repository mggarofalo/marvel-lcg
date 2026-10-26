using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class GreenGoblinVictoryTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("risky_business", 1u)]
    public void PublicSetupWinsWithExactDecisionReplay(string scenario, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var specification = new GameSpecification(scenario, ["captain_america"], null, seed);
        var game = factory.Create(specification).Game;
        var replay = factory.Create(specification).Game;
        var policy = new GoblinTacticalPolicy(game.State.Facts);
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is not null)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}: {game.Pending.Label}");
            var answer = policy.Answer(game);
            var identity = game.State.Seats[0].IdentityCard;
            output.WriteLine($"{policy.Answered}: r{game.Round} {identity.FaceId} dmg={identity.Damage}; "
                + $"{game.Pending.Label}; {game.Pending.Affordances.FirstOrDefault(option => option.Id == answer.Affordance)?.Label ?? "decline"}; "
                + $"targets={string.Join(',', answer.Targets)}");
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}, seed={seed}, decision={policy.Answered}, "
                    + $"prompt={game.Pending.Label}, answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            Assert.Equal(Observe(game, result), Observe(replay, replayed));
        }
        output.WriteLine($"{scenario}, seed={seed}: {game.State.Result}, round={game.Round}, "
            + $"decisions={policy.Answered}, recoveries={policy.Recoveries}, allyDefenses={policy.AllyDefenses}");
        Assert.Equal(Outcome.PlayersWin, game.State.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(replay.Pending);
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
