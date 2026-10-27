using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged Kree Fanatic public games with exact accepted-decision replay.</summary>
public sealed class RonanPublicGameTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("rhino", false, 1u)]
    [InlineData("rhino_expert", false, 2u)]
    [InlineData("rhino", true, 3u)]
    [InlineData("rhino_expert", true, 4u)]
    public void PublicGamesCompleteWithExactAcceptedDecisionReplay(string scenario, bool multiplayer, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        Assert.Contains(factory.DiscoverSetup().ModularSets, set => set.Key == "kree_fanatic");
        string[] heroes = multiplayer ? ["captain_marvel", "spider_man"] : ["spider_man"];
        var specification = new GameSpecification(scenario, heroes, ["kree_fanatic"], seed);
        var opened = factory.Create(specification);
        var game = opened.Game;
        Assert.Equal(["90001", "90002", "90003", "90004", "90004", "90005", "90005"],
            game.State.Cards.Where(card => RonanGameEvidence.IsRonan(card.FaceId))
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var response = host.Exchange(EngineRequest.OpenGame("open", "ronan-game", specification));
        Assert.Null(response.Error);
        string capability = response.Capability!;
        var initial = Assert.Single(store.Load()).Save.Initial;
        Assert.Equal(opened.SetupEvents.Select(JournalJson.Event).Select(item => item.GetRawText()),
            initial.Events.Select(item => item.GetRawText()));
        Assert.Equal(game.State.Random.Generator.WordsConsumed, initial.RngWords);
        Assert.Equal(game.State.Digest().Canonical(), initial.StateDigest);
        var policy = new CoreGamePolicy(game.State.Facts);
        var evidence = new RonanGameEvidence(output);
        using var trace = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        while (game.Pending is { } prompt)
        {
            string context = $"{scenario}, multiplayer={multiplayer}, seed={seed}, "
                + $"decision={policy.Answered + 1}, round={game.Round}; "
                + $"prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}";
            Assert.True(policy.Answered < 2000, context);
            Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(prompt)),
                JsonSerializer.Serialize(PromptRecord.From(response.Prompt!)));
            string before = Observe(game);
            var agenda = game.State.Agenda.Outstanding.ToArray();
            var effects = game.State.Effects.Registered.ToArray();
            Decision answer;
            Resolution result;
            try
            {
                answer = policy.Answer(game);
                Assert.Equal(before, Observe(game));
                Assert.Equal(agenda, game.State.Agenda.Outstanding);
                Assert.Equal(effects, game.State.Effects.Registered);
                Assert.True(DecisionAssessment.Assess(prompt, answer).IsAccepted, context);
                evidence.Before(game, answer, policy.Answered);
                result = game.Resolve(answer);
                evidence.Resolved(game, result, policy.Answered);
            }
            catch (Exception failure)
            {
                output.WriteLine("Accepted public-session prefix: " + Archive(store, scenario, seed));
                throw new InvalidOperationException(context, failure);
            }
            response = host.Exchange(EngineRequest.ResolveGame("answer-" + policy.Answered,
                "ronan-game", capability, answer, response.Revision));
            if (response.Error is not null)
                output.WriteLine("Rejected public-session prefix: " + Archive(store, scenario, seed));
            Assert.True(response.Error is null, context + "; host=" + JsonSerializer.Serialize(response.Error));
            var step = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).Last();
            Assert.Equal(game.State.Digest().Fingerprint(), step.StateFingerprint);
            Assert.Equal(game.State.Random.Generator.WordsConsumed, step.RngWords);
            Assert.Equal(result.Events.Select(JournalJson.Event).Select(item => item.GetRawText()),
                step.Events.Select(item => item.GetRawText()));
            Assert.Equal(JsonSerializer.Serialize(SessionReplay.Result(game)), JsonSerializer.Serialize(step.Result));
            trace.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(answer) + Observe(game, result)));
        }
        string archive = Archive(store, scenario, seed);
        evidence.Finish(game, heroes.Length);
        Assert.Null(response.Prompt);
        Assert.True(policy.CardsPlayed > 0);
        Assert.True(policy.PlayerAttacks > 0);
        Assert.True(policy.Payments > 0);
        // The test chooses fixed seeds and ordinary starters; terminal defeat is a complete modular game.
        var save = SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal("kree_fanatic", Assert.Single(save.Setup.ModularSets!));
        var replay = SessionReplay.Verify(save, factory.Compatibility, setup =>
        {
            var dealt = factory.Create(new GameSpecification(setup.Scenario, setup.Heroes,
                setup.ModularSets, setup.Seed, setup.PlayerDecks, setup.Campaign));
            return new ReplayOpenedGame(dealt.Game, dealt.SetupEvents);
        });
        Assert.Equal(Observe(game), Observe(replay));
        output.WriteLine($"{scenario}, heroes={string.Join(',', heroes)}, seed={seed}: "
            + $"{game.State.Result}; round={game.Round}, decisions={policy.Answered}; {evidence.Summary}; "
            + $"rngWords={game.State.Random.Generator.WordsConsumed}; finalSha256={Hash(game.State.Digest().Canonical())}; "
            + $"traceSha256={Convert.ToHexString(trace.GetHashAndReset())}; archive={archive}");
    }

    private static string Archive(MemorySessionStore store, string scenario, uint seed)
    {
        string directory = Path.Combine(Path.GetTempPath(), "marvel-ronan-public-games");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"{scenario}-{seed}.session.json");
        File.WriteAllText(path, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        return path;
    }

    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(), RngWords = game.State.Random.Generator.WordsConsumed,
        RngState = game.State.Random.Generator.GetState().Words,
        RngIndex = game.State.Random.Generator.GetState().Index,
        game.State.Result, game.Phase, game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events, result?.Information,
    });
}
