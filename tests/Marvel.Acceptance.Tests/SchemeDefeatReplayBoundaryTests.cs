using System.Security.Cryptography;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
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
    [Rule("rr:forced.5")]
    [Rule("rr:you-your.7")]
    [Rule("rr:indirect-damage.1")]
    public void CurrentLegalSupportDefeatGameCompletesAndReplaysEveryNewFact()
    {
        var (lines, game) = OpenRecordedGame();
        var journal = new CurrentLegalJournal();
        // These are explicit current-game intentions derived from the frozen
        // choices, not acceptance of its changed history. Backflip answers the
        // minion's positive damage at 31.
        for (int index = 0; index < 73; index++)
        {
            var source = ReadStep(lines, index);
            var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(game.Pending);
            if (index == 31) Assert.Equal(79, game.State.Attack!.Enemy);
            var durable = DecisionFor(game, source);
            var result = game.Resolve(durable.Resolve(prompt));
            journal.Record(game, prompt, durable, result.Events);
            if (index == 49)
            {
                Assert.Equal("01064: choose a card", prompt.Label);
                Assert.Equal("04070", source.Decision.Label);
                // Exact scheme-defeat events retain the support/controller
                // attribution, Crossbones activation and resulting threat.
                JournalReplay.RequireEvents(source.Events, result.Events, "support defeats Crossbones' Assault");
            }
            if (index == 72) AssertSimultaneousForcedInterrupts(game, source, result.Events);
        }

        // rr:forced.5: the first player orders simultaneous forced abilities. Laser
        // Rifle first keeps its recorded resolution ahead of the newly firing gun.
        var order = game.Pending!;
        var rifleFirst = new DecisionSelector(false, 66, -1, "Laser Rifle", "Laser Rifle", 0)
        {
            AnchorKind = AffordanceAnchorKind.Card,
        };
        var ordered = new DurableDecision(DurableDecision.SimulationActor(order, rifleFirst), rifleFirst, [], [],
            new Dictionary<string, long>(), []);
        Assert.Equal(game.State.FirstPlayer, ordered.Actor);
        var resolved = game.Resolve(ordered.Resolve(order));
        journal.Record(game, order, ordered, resolved.Events);
        AssertRifleThenMachineGun(game, ReadStep(lines, 72), resolved.Events);

        // The gun's discard shifts every later encounter card, so the recorded
        // intentions stop applying. The acting policy completes the current game.
        var header = SimulationRecordContract.Read<HeaderRecord>(lines[0], "header");
        journal.CompleteWithActingPolicy(game, RepositoryRoot(), header.PolicySeed, header.DecisionLimit);
        Assert.Equal(77, journal.Count);
        Assert.Equal(Outcome.PlayersLose, game.State.Result);
        string currentTerminal = game.State.Digest().Canonical();

        var (_, replay) = OpenRecordedGame();
        journal.Replay(replay);
        Assert.Equal(game.State.Result, replay.State.Result);
        Assert.Equal(currentTerminal, replay.State.Digest().Canonical());
    }

    private static void AssertSimultaneousForcedInterrupts(Game game, StepRecord step, IReadOnlyList<GameEvent> events)
    {
        // Crossbones II's reveal attached the gun at 68; this is his first attack since.
        // Laser Rifle alone resolved without a prompt in the recording.
        Assert.Equal(13, step.Events.Count);
        Assert.Equal(12, events.Count);
        JournalReplay.RequireEvents(step.Events.Take(12).ToList(), events, "current source 72 prefix");
        var attack = Assert.IsType<AttackBoundary>(events[11]);
        Assert.Equal((52, 1), (attack.Source, attack.Target));
        var order = game.Pending!;
        Assert.Equal(Marvel.Rules.Prompts.Question.Order, order.Asking);
        Assert.Equal(game.State.FirstPlayer, order.Player);
        Assert.Equal([53, 66], order.Affordances.Select(option => option.AnchorId).Order());
        Assert.Equal("04064", game.State.Cards[53].FaceId);
        Assert.Equal("04072", game.State.Cards[66].FaceId);
        var failure = Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequireEvents(step.Events, events, "step 72 events"));
        Assert.Equal("step 72 events diverged: expected 13, got 12", failure.Message);
    }

    private static void AssertRifleThenMachineGun(Game game, StepRecord recorded, IReadOnlyList<GameEvent> events)
    {
        Assert.Equal(6, events.Count);
        JournalReplay.RequireEvents(recorded.Events.Skip(12).ToList(), events.Take(1).ToList(), "Laser Rifle");
        var ammo = Assert.IsType<FieldSet>(events[1]);
        Assert.Equal((53, "c_ammo", 2, 1), (ammo.Card, ammo.Field, ammo.From, ammo.To));
        var discarded = Assert.IsType<CardsMoved>(events[2]);
        Assert.Equal(("EncounterDeck", "EncounterDiscardPile"), (discarded.From.Zone, discarded.To.Zone));
        Assert.Equal(78, Assert.Single(discarded.Cards).Card);
        Assert.IsType<ActivityCompleted>(events[3]);
        // "you" is the attacked player (rr:you-your.7); the discarded card's boost
        // icons become Spider-Man's indirect damage, since he controls no other character.
        var damage = Assert.IsType<FieldSet>(events[4]);
        Assert.Equal((1, "health", 5, 2), (damage.Card, damage.Field, damage.From, damage.To));
        Assert.Equal(53, Assert.IsType<AbilityWindowResult>(events[5]).Source);
        Assert.Equal("Spider-Sense", Assert.Single(game.Pending!.Affordances).Verb);
    }

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
