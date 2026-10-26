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

/// <summary>Unarranged admitted Black Widow games with an independent public session and exact replay.</summary>
public sealed class BlackWidowPublicGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("rhino", false, false, 1u)]
    [InlineData("klaw", false, false, 2u)]
    [InlineData("ultron", false, false, 3u)]
    [InlineData("crossbones", false, false, 4u)]
    [InlineData("absorbing_man", false, false, 5u)]
    [InlineData("rhino", true, false, 6u)]
    [InlineData("absorbing_man", true, false, 7u)]
    public void PublicGamesCompleteWithExactAcceptedDecisionReplay(
        string scenario, bool multiplayer, bool custom, uint seed)
        => Play(scenario, multiplayer, custom, seed);

    [Fact]
    public void PublicAggressionPreparationDeckCompletesWithExactAcceptedDecisionReplay()
        => Play("rhino", false, true, 1);

    private void Play(string scenario, bool multiplayer, bool custom, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var widow = catalog.Heroes.Single(hero => hero.Key == "black_widow");
        string[] deck = custom
            ? [.. widow.Signature, .. catalog.Heroes.Single(hero => hero.Key == "she_hulk")
                .Starter.Skip(widow.Signature.Count).SkipLast(3), "08030", "08030", "08030"]
            : [.. widow.Starter];
        Assert.Equal(40, deck.Length);
        Assert.True(factory.AssessDeck(widow.Key, deck).IsValid);
        string[] heroes = multiplayer ? ["black_widow", "captain_marvel"] : ["black_widow"];
        var specification = new GameSpecification(scenario, heroes, null, seed,
            custom ? [new(deck)] : null);
        var opened = factory.Create(specification);
        var game = opened.Game;
        Assert.Equal(deck.Order(StringComparer.Ordinal), game.State.Cards.Where(card => card.Owner == 0
            && game.State.Facts.Kind(card.FaceId) is CardKind.Ally or CardKind.Event or CardKind.Support
                or CardKind.Upgrade or CardKind.Resource).Select(card => card.FaceId).Order(StringComparer.Ordinal));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var response = host.Exchange(EngineRequest.OpenGame("open", "widow-game", specification));
        Assert.Null(response.Error);
        string capability = response.Capability!;
        var initial = Assert.Single(store.Load()).Save.Initial;
        Assert.Equal(opened.SetupEvents.Select(JournalJson.Event).Select(item => item.GetRawText()),
            initial.Events.Select(item => item.GetRawText()));
        Assert.Equal(game.State.Random.Generator.WordsConsumed, initial.RngWords);
        Assert.Equal(game.State.Digest().Canonical(), initial.StateDigest);
        var policy = new BlackWidowGamePolicy(game.State.Facts);
        var evidence = new BlackWidowGameEvidence(output);
        using var trace = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        while (game.Pending is { } prompt)
        {
            string context = $"{scenario}, multiplayer={multiplayer}, custom={custom}, seed={seed}, "
                + $"decision={policy.Answered + 1}, round={game.Round}; "
                + $"prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}";
            Assert.True(policy.Answered < 2000, context);
            Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(prompt)),
                JsonSerializer.Serialize(PromptRecord.From(response.Prompt!)));
            string before = Observe(game);
            Decision answer;
            Resolution result;
            try
            {
                answer = policy.Answer(game);
                Assert.Equal(before, Observe(game));
                var after = evidence.Before(game, answer, policy.Answered);
                result = game.Resolve(answer);
                after();
            }
            catch (Exception failure) { throw new InvalidOperationException(context, failure); }
            response = host.Exchange(EngineRequest.ResolveGame("answer-" + policy.Answered,
                "widow-game", capability, answer, response.Revision));
            Assert.True(response.Error is null, context + "; host=" + JsonSerializer.Serialize(response.Error));
            var step = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).Last();
            Assert.Equal(game.State.Digest().Fingerprint(), step.StateFingerprint);
            Assert.Equal(game.State.Random.Generator.WordsConsumed, step.RngWords);
            Assert.Equal(result.Events.Select(JournalJson.Event).Select(item => item.GetRawText()),
                step.Events.Select(item => item.GetRawText()));
            Assert.Equal(JsonSerializer.Serialize(SessionReplay.Result(game)), JsonSerializer.Serialize(step.Result));
            trace.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(answer) + Observe(game, result)));
        }
        evidence.Finish(game, heroes.Length, custom);
        Assert.Null(response.Prompt);
        // Strict durable JSON plus the production replay verifier reconstructs the complete accepted trace.
        var save = SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        var replay = SessionReplay.Verify(save, factory.Compatibility, setup =>
        {
            var dealt = factory.Create(new GameSpecification(setup.Scenario, setup.Heroes,
                setup.ModularSets, setup.Seed, setup.PlayerDecks, setup.Campaign));
            return new ReplayOpenedGame(dealt.Game, dealt.SetupEvents);
        });
        Assert.Equal(Observe(game), Observe(replay));
        output.WriteLine($"{scenario}, heroes={string.Join(',', heroes)}, custom={custom}, seed={seed}: "
            + $"{game.State.Result}; round={game.Round}, decisions={policy.Answered}; {evidence.Summary}; "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}; finalSha256={Hash(game.State.Digest().Canonical())}; "
            + $"traceSha256={Convert.ToHexString(trace.GetHashAndReset())}; deck={string.Join(',', deck)}");
    }

    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(), RngWords = game.State.Random.Generator.WordsConsumed,
        game.State.Result, game.Phase, game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events, result?.Information,
    });
}
