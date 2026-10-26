using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged published Thor starters through admitted setup and exact decision replay.</summary>
public sealed class ThorPublicGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("klaw", false, 2u)]
    [InlineData("ultron", false, 3u)]
    [InlineData("crossbones", false, 4u)]
    [InlineData("absorbing_man", false, 5u)]
    [InlineData("rhino", true, 6u)]
    [InlineData("rhino", false, 7u)]
    [InlineData("rhino", false, 8u)]
    [InlineData("rhino", false, 9u)]
    [InlineData("rhino", false, 10u)]
    public void PublicStarterCompletesWithExactDecisionReplay(string scenario, bool multiplayer, uint seed)
        => Play(scenario, multiplayer, seed);

    [Fact]
    public void HammerThrowPromotionCompletesWithExactDecisionReplay()
        => Play("rhino", false, 1);

    private void Play(string scenario, bool multiplayer, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        string[] heroes = multiplayer ? ["thor", "captain_marvel"] : ["thor"];
        var starter = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "thor").Starter;
        Assert.Equal(40, starter.Count);
        Assert.True(factory.AssessDeck("thor", starter).IsValid);
        var specification = new GameSpecification(scenario, heroes, null, seed);
        var game = factory.Create(specification).Game;
        var replay = factory.Create(specification).Game;
        AssertStarter(game, starter);
        var policy = new ThorTacticalPolicy(game.State.Facts);
        var evidence = new ThorGameEvidence(output);
        using var trace = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 2000, $"{scenario}, seed={seed}: {prompt.Label}");
            string before = Observe(game);
            var answer = policy.Answer(game);
            Assert.Equal(before, Observe(game));
            var after = evidence.Before(game, answer, policy.Answered);
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"{scenario}, multiplayer={multiplayer}, seed={seed}, "
                    + $"decision={policy.Answered}, round={game.Round}; prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}; "
                    + $"answer={JsonSerializer.Serialize(answer)}", failure);
            }
            var replayed = replay.Resolve(answer);
            after();
            string observed = Observe(game, result);
            Assert.Equal(observed, Observe(replay, replayed));
            trace.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(answer) + observed));
        }
        evidence.Finish(game, heroes.Length);
        Assert.Null(replay.Pending);
        output.WriteLine($"{scenario}, heroes={string.Join(',', heroes)}, seed={seed}: {game.State.Result}; "
            + $"round={game.Round}, decisions={policy.Answered}, {evidence.Summary}; "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}; {evidence.Cards}; "
            + $"finalSha256={Hash(game.State.Digest().Canonical())}; "
            + $"traceSha256={Convert.ToHexString(trace.GetHashAndReset())}; starter={string.Join(',', starter)}");
    }

    private static void AssertStarter(Game game, IReadOnlyList<string> starter)
    {
        // Ultron's setup uses a physical starter card as the initial drone.
        Assert.Equal(starter.Order(StringComparer.Ordinal), game.State.Cards
            .Where(card => card.Owner == 0 && game.State.Facts.Kind(card.FaceId)
                is CardKind.Ally or CardKind.Event or CardKind.Support or CardKind.Upgrade or CardKind.Resource)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
    }

    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

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
