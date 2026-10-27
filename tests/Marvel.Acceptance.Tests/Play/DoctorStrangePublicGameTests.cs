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

/// <summary>Unarranged admitted Doctor Strange games with an independent public session and exact replay.</summary>
public sealed class DoctorStrangePublicGameTests(ITestOutputHelper output)
{
    // Pinned mc08 divider: 15 signature, 17 Protection and 8 Basic cards.
    private static readonly string[] PublishedStarter =
    {
        "09002", "09003", "09003", "09004", "09004", "09005", "09005", "09006",
        "09007", "09007", "09008", "09009", "09010", "09010", "09011", "09012",
        "09013", "09014", "09015", "09015", "09015", "09016", "09016", "09016",
        "09017", "09017", "09018", "09018", "09019", "09020", "09020", "09020",
        "09021", "09021", "09021", "09022", "09023", "09024", "09025", "09026",
    };

    [Fact]
    public void PolicyUsesTheOfferedNaturalTalentBeforeChangingForm()
    {
        var game = DatasetGameFactory.Load(RepositoryPaths.Root)
            .Create(new GameSpecification("rhino", ["doctor_strange"], null, 1)).Game;
        var policy = new DoctorStrangeGamePolicy(game.State.Facts);
        game.Resolve(policy.Answer(game));
        var prompt = game.Pending!;
        var natural = Assert.Single(prompt.Affordances,
            option => option.Label == "Natural Talent" && option.IsLegal);
        // Test-policy choice: use the offered alter-ego ability before changing form.
        string before = Observe(game);
        var answer = policy.Answer(game);
        Assert.Equal(before, Observe(game));
        Assert.Equal(natural.Id, answer.Affordance);
        Assert.True(DecisionAssessment.Assess(prompt, answer).IsAccepted);
    }

    [Theory]
    [InlineData("rhino", false, false, 1u)]
    [InlineData("klaw", false, false, 2u)]
    [InlineData("ultron", false, false, 3u)]
    [InlineData("crossbones", false, false, 4u)]
    [InlineData("absorbing_man", false, false, 5u)]
    [InlineData("rhino_expert", false, false, 6u)]
    [InlineData("absorbing_man_expert", true, false, 7u)]
    [InlineData("rhino", true, true, 8u)]
    public void PublicGamesCompleteWithExactAcceptedDecisionReplay(
        string scenario, bool multiplayer, bool custom, uint seed)
        => Play(scenario, multiplayer, custom, seed);

    private void Play(string scenario, bool multiplayer, bool custom, uint seed)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var strange = catalog.Heroes.Single(hero => hero.Key == "doctor_strange");
        Assert.Equal(PublishedStarter, strange.Starter);
        string[] deck = custom
            ? [.. strange.Signature, .. catalog.Heroes.Single(hero => hero.Key == "she_hulk")
                .Starter.Skip(strange.Signature.Count).SkipLast(3), "09037", "09037", "09037"]
            : [.. strange.Starter];
        Assert.Equal(40, deck.Length);
        Assert.True(factory.AssessDeck(strange.Key, deck).IsValid);
        string[] heroes = multiplayer ? ["captain_marvel", "doctor_strange"] : ["doctor_strange"];
        int strangeSeat = Array.IndexOf(heroes, "doctor_strange");
        var decks = custom
            ? new Marvel.Rules.Setup.PlayerDeckRecipe[]
                { new(catalog.Heroes.Single(hero => hero.Key == "captain_marvel").Starter), new(deck) }
            : null;
        var specification = new GameSpecification(scenario, heroes, null, seed, decks);
        var opened = factory.Create(specification);
        var game = opened.Game;
        Assert.Equal(deck.Order(StringComparer.Ordinal), game.State.Cards.Where(card => card.Owner == strangeSeat
            && !game.State.Facts.Traits(card.FaceId).Contains("INVOCATION")
            && game.State.Facts.Kind(card.FaceId) is CardKind.Ally or CardKind.Event or CardKind.Support
                or CardKind.Upgrade or CardKind.Resource).Select(card => card.FaceId).Order(StringComparer.Ordinal));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var response = host.Exchange(EngineRequest.OpenGame("open", "strange-game", specification));
        Assert.Null(response.Error);
        string capability = response.Capability!;
        var initial = Assert.Single(store.Load()).Save.Initial;
        Assert.Equal(opened.SetupEvents.Select(JournalJson.Event).Select(item => item.GetRawText()),
            initial.Events.Select(item => item.GetRawText()));
        Assert.Equal(game.State.Random.Generator.WordsConsumed, initial.RngWords);
        Assert.Equal(game.State.Digest().Canonical(), initial.StateDigest);
        var policy = new DoctorStrangeGamePolicy(game.State.Facts);
        var evidence = new DoctorStrangeGameEvidence(output);
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
                evidence.Resolved(game, result);
            }
            catch (Exception failure)
            {
                string directory = Path.Combine(Path.GetTempPath(), "marvel-strange-public-game-failures");
                Directory.CreateDirectory(directory);
                string prefix = Path.Combine(directory, $"{scenario}-{seed}-{multiplayer}-{custom}");
                File.WriteAllText(prefix + ".session.json", SessionSaveJson.Write(Assert.Single(store.Load()).Save));
                output.WriteLine("Accepted public-session prefix: " + prefix + ".session.json");
                throw new InvalidOperationException(context, failure);
            }
            response = host.Exchange(EngineRequest.ResolveGame("answer-" + policy.Answered,
                "strange-game", capability, answer, response.Revision));
            Assert.True(response.Error is null, context + "; host=" + JsonSerializer.Serialize(response.Error));
            var step = Assert.Single(store.Load()).Save.Units.SelectMany(unit => unit.Decisions).Last();
            Assert.Equal(game.State.Digest().Fingerprint(), step.StateFingerprint);
            Assert.Equal(game.State.Random.Generator.WordsConsumed, step.RngWords);
            Assert.Equal(result.Events.Select(JournalJson.Event).Select(item => item.GetRawText()),
                step.Events.Select(item => item.GetRawText()));
            Assert.Equal(JsonSerializer.Serialize(SessionReplay.Result(game)), JsonSerializer.Serialize(step.Result));
            trace.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(answer) + Observe(game, result)));
        }
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
