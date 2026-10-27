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

/// <summary>Unarranged admitted Hulk games with an independent public session and exact replay.</summary>
public sealed class HulkPublicGameTests(ITestOutputHelper output)
{
    [Fact]
    public void PolicyUsesOfferedExperimentalResearchBeforeChangingForm()
    {
        var game = DatasetGameFactory.Load(RepositoryPaths.Root)
            .Create(new GameSpecification("rhino", ["hulk"], null, 1)).Game;
        var policy = new HulkGamePolicy(game.State.Facts);
        game.Resolve(policy.Answer(game));
        var prompt = game.Pending!;
        var research = Assert.Single(prompt.Affordances,
            option => option.Timing is not null && option.IsLegal
                && game.State.Cards[option.AnchorId].FaceId == "10001b");
        // Test-policy choice: use the offered alter-ego ability before changing form.
        string before = Observe(game);
        var answer = policy.Answer(game);
        Assert.Equal(before, Observe(game));
        Assert.Equal(research.Id, answer.Affordance);
        Assert.True(DecisionAssessment.Assess(prompt, answer).IsAccepted);
    }

    [Theory]
    [InlineData("rhino", false, false, 1u)]
    [InlineData("klaw", false, false, 2u)]
    [InlineData("ultron", false, false, 3u)]
    [InlineData("crossbones", false, false, 4u)]
    [InlineData("absorbing_man", false, false, 5u)]
    [InlineData("rhino_expert", false, false, 6u)]
    [InlineData("rhino", true, false, 7u)]
    public void PublicGamesCompleteWithExactAcceptedDecisionReplay(
        string scenario, bool multiplayer, bool custom, uint seed)
        => Play(scenario, multiplayer, custom, seed);

    [Fact]
    public void PublicCustomExtrasDeckCompletesWithExactAcceptedDecisionReplay()
        => Play("rhino", true, true, 8);

    private void Play(string scenario, bool multiplayer, bool custom, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hulk = catalog.Heroes.Single(hero => hero.Key == "hulk");
        string[] deck = custom
            ? [.. hulk.Starter.SkipLast(3), "10032", "10032", "10032"]
            : [.. hulk.Starter];
        Assert.Equal(40, deck.Length);
        Assert.True(factory.AssessDeck(hulk.Key, deck).IsValid);
        string[] heroes = multiplayer ? ["captain_marvel", "hulk"] : ["hulk"];
        int hulkSeat = Array.IndexOf(heroes, "hulk");
        var decks = custom
            ? new Marvel.Rules.Setup.PlayerDeckRecipe[]
                { new(catalog.Heroes.Single(hero => hero.Key == "captain_marvel").Starter), new(deck) }
            : null;
        var specification = new GameSpecification(scenario, heroes, null, seed,
            decks);
        var opened = factory.Create(specification);
        var game = opened.Game;
        Assert.Equal(deck.Order(StringComparer.Ordinal), game.State.Cards.Where(card => card.Owner == hulkSeat
            && game.State.Facts.Kind(card.FaceId) is CardKind.Ally or CardKind.Event or CardKind.Support
                or CardKind.Upgrade or CardKind.Resource).Select(card => card.FaceId).Order(StringComparer.Ordinal));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var response = host.Exchange(EngineRequest.OpenGame("open", "hulk-game", specification));
        Assert.Null(response.Error);
        string capability = response.Capability!;
        var initial = Assert.Single(store.Load()).Save.Initial;
        Assert.Equal(opened.SetupEvents.Select(JournalJson.Event).Select(item => item.GetRawText()),
            initial.Events.Select(item => item.GetRawText()));
        Assert.Equal(game.State.Random.Generator.WordsConsumed, initial.RngWords);
        Assert.Equal(game.State.Digest().Canonical(), initial.StateDigest);
        var policy = new HulkGamePolicy(game.State.Facts);
        var evidence = new HulkGameEvidence(output, hulkSeat);
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
                var after = evidence.Before(game, answer, policy.Answered);
                result = game.Resolve(answer);
                after();
                evidence.Resolved(game, result);
            }
            catch (Exception failure)
            {
                output.WriteLine("Accepted public-session prefix: " + Archive(store, scenario, seed, multiplayer, custom));
                throw new InvalidOperationException(context, failure);
            }
            response = host.Exchange(EngineRequest.ResolveGame("answer-" + policy.Answered,
                "hulk-game", capability, answer, response.Revision));
            Assert.True(response.Error is null, context + "; host=" + JsonSerializer.Serialize(response.Error));
            var step = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).Last();
            Assert.Equal(game.State.Digest().Fingerprint(), step.StateFingerprint);
            Assert.Equal(game.State.Random.Generator.WordsConsumed, step.RngWords);
            Assert.Equal(result.Events.Select(JournalJson.Event).Select(item => item.GetRawText()),
                step.Events.Select(item => item.GetRawText()));
            Assert.Equal(JsonSerializer.Serialize(SessionReplay.Result(game)), JsonSerializer.Serialize(step.Result));
            trace.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(answer) + Observe(game, result)));
        }
        string archive = Archive(store, scenario, seed, multiplayer, custom);
        evidence.Finish(game, heroes.Length);
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
            + $"traceSha256={Convert.ToHexString(trace.GetHashAndReset())}; deck={string.Join(',', deck)}; archive={archive}");
    }

    private static string Archive(MemorySessionStore store, string scenario, uint seed, bool multiplayer, bool custom)
    {
        string directory = Path.Combine(Path.GetTempPath(), "marvel-hulk-public-games");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"{scenario}-{seed}-{multiplayer}-{custom}.session.json");
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
