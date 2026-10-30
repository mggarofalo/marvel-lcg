using System.Security.Cryptography;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SchemeDefeatReplayBoundaryTests : SimulationHarnessTestBase
{
    [Fact]
    public void FrozenSupportGameRejectsAtFirstZeroDamageInterruptBoundary()
    {
        string path = FixturePath();
        Assert.Equal("74EC92E18D5251C951F1884842905BA60FE4A133D3DF8258F23514C2B257F979",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        var (lines, game) = OpenRecordedGame();
        for (int index = 0; index < 30; index++)
        {
            var step = ReadStep(lines, index);
            if (index == 21)
            {
                // The first recorded Power of Justice offer is the projected spelling.
                var strict = Assert.Throws<ReplayDivergenceException>(() =>
                    JournalReplay.RequirePrompt(step.Prompt, game.Pending!, "strict step 21"));
                Assert.Contains("\"effect\":30,\"generates\":\"GG\"", strict.Message, StringComparison.Ordinal);
            }
            FrozenPromptCompatibility.Require(step.Prompt, game.Pending!, game.State, $"step {index}");
            var result = game.Resolve(DecisionFor(game, step).Resolve(game.Pending!));
            JournalReplay.RequireEvents(step.Events, result.Events, $"step {index}");
            JournalReplay.RequireFingerprint(step.Digest, game.State.Digest().Fingerprint(), $"step {index}");
        }

        var boundary = ReadStep(lines, 30);
        FrozenPromptCompatibility.Require(boundary.Prompt, game.Pending!, game.State, "step 30");
        var changed = game.Resolve(DecisionFor(game, boundary).Resolve(game.Pending!));
        // The recorded boost/defense prefix is intact. Zero damage completes
        // Crossbones' attack, then starts the engaged minion's distinct attack.
        Assert.Equal(4, boundary.Events.Count);
        Assert.Equal(7, changed.Events.Count);
        JournalReplay.RequireEvents(boundary.Events, changed.Events.Take(4).ToList(), "step 30 prefix");
        var completion = Assert.IsType<AttackCompleted>(changed.Events[4]);
        Assert.Equal(51, completion.Enemy);
        Assert.Equal(1, completion.Defender);
        Assert.Equal(0, completion.DamageDealt);
        var finished = Assert.IsType<AttackBoundary>(changed.Events[5]);
        Assert.Equal(51, finished.Source);
        Assert.False(finished.Started);
        var started = Assert.IsType<AttackBoundary>(changed.Events[6]);
        Assert.Equal(79, started.Source);
        Assert.True(started.Started);
        Assert.Equal(79, game.State.Attack!.Enemy);
        Assert.Equal("Backflip", ReadStep(lines, 31).Decision.Verb);

        // The research policy is not an admitted simulation-replay policy.
        // Its recorded decisions use the production journal comparison directly.
        var failure = Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequireEvents(boundary.Events, changed.Events, "step 30 events"));
        Assert.Contains("step 30 events diverged: expected 4, got 7", failure.Message);
    }

    [Fact]
    public void CurrentLegalSupportDefeatGameCompletesAndReplaysEveryNewFact()
    {
        var (lines, game) = OpenRecordedGame();
        var journal = new List<JournalStep>();
        // These are explicit current-game intentions derived from the frozen
        // choices, not acceptance of its changed history. Backflip answers the
        // minion's positive damage at 31. At 75/76 choose that minion first, then
        // use Backflip: the preceding defended Crossbones attack has zero damage.
        foreach (int index in Enumerable.Range(0, 90).Select(CurrentDecisionIndex))
        {
            var source = ReadStep(lines, index);
            var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(game.Pending);
            if (index == 31) Assert.Equal(79, game.State.Attack!.Enemy);
            var durable = DecisionFor(game, source);
            var result = game.Resolve(durable.Resolve(prompt));
            journal.Add(new JournalStep(PromptRecord.From(prompt), durable,
                result.Events.Select(JournalJson.Event).ToList(),
                game.State.Random.Generator.WordsConsumed, game.State.Digest().Fingerprint()));
            if (index == 49)
            {
                Assert.Equal("01064: choose a card", prompt.Label);
                Assert.Equal("04070", source.Decision.Label);
                // Exact scheme-defeat events retain the support/controller
                // attribution, Crossbones activation and resulting threat.
                JournalReplay.RequireEvents(source.Events, result.Events, "support defeats Crossbones' Assault");
            }
        }
        Assert.Equal(90, journal.Count);
        Assert.Null(game.Pending);
        var terminal = SimulationRecordContract.Read<ResultRecord>(lines[92], "result");
        Assert.Equal(terminal.Outcome, game.State.Result.ToString());
        string currentTerminal = game.State.Digest().Canonical();

        var (_, replay) = OpenRecordedGame();
        foreach (var step in journal)
        {
            JournalReplay.RequirePrompt(step.Prompt, replay.Pending!, "current replay prompt");
            var result = replay.Resolve(step.Decision.Resolve(replay.Pending!));
            JournalReplay.RequireEvents(step.Events, result.Events, "current replay events");
            JournalReplay.RequireRng(step.RngWords, replay.State.Random.Generator.WordsConsumed, "current replay RNG");
            JournalReplay.RequireFingerprint(step.StateFingerprint, replay.State.Digest().Fingerprint(), "current replay state");
        }
        Assert.Null(replay.Pending);
        Assert.Equal(game.State.Result, replay.State.Result);
        Assert.Equal(currentTerminal, replay.State.Digest().Canonical());
    }

    private static int CurrentDecisionIndex(int index) => index switch { 75 => 76, 76 => 75, _ => index };

    private static string FixturePath() => Path.Combine(RepositoryRoot(), "tests", "Marvel.Acceptance.Tests",
        "Fixtures", "scheme-defeat-10.jsonl.gz");

    private static StepRecord ReadStep(string[] lines, int index) =>
        SimulationRecordContract.Read<StepRecord>(lines[index + 2], "step");

    private static DurableDecision DecisionFor(Game game, StepRecord step) => new(
        DurableDecision.SimulationActor(game.Pending!, step.Decision), step.Decision,
        step.Targets, step.Resources, step.Values, step.Allocations);

    private static (string[] Lines, Game Game) OpenRecordedGame()
    {
        string[] lines = [.. SimulationRecordFiles.ReadLines(FixturePath())];
        var header = SimulationRecordContract.Read<HeaderRecord>(lines[0], "header");
        var start = SimulationRecordContract.Read<StartRecord>(lines[1], "start");
        var config = new SimulationConfig(header.Scenario, header.Difficulty, header.Heroes,
            header.ModularSets, 1, [start.Seed], null, null, header.PolicySeed,
            header.DecisionLimit, null, RepositoryRoot());
        var opened = SimulationHarnessSupport.Open(SimulationData.Load(RepositoryRoot()), config, start.Seed);
        Assert.Equal(start.InitialDigest, opened.Game.State.Digest().Canonical());
        JournalReplay.RequireEvents(start.SetupEvents, opened.SetupEvents, "setup");
        return (lines, opened.Game);
    }
}
