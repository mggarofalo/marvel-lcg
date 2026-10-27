using System.Security.Cryptography;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Sim;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SchemeDefeatGameRegressionTests : SimulationHarnessTestBase
{
    [Fact]
    [Rule("rr:status-cards.2")]
    [Rule("rr:stun-stunned.5")]
    [Rule("rr:stun-stunned.7")]
    [Rule("rr:cancel.2")]
    [Rule("rr:cancel.3")]
    public void FrozenAllyDefeatGameRejectsAtFirstPaidStunCancellation()
    {
        var (lines, game) = OpenRecordedGame();
        ReplayPrefix(lines, game, 16);
        var step = ReadStep(lines, 16);
        JournalReplay.RequirePrompt(step.Prompt, game.Pending!, "step 16");
        Assert.Equal("Haymaker", step.Decision.Label);
        Assert.Equal(41, step.Decision.AnchorId);
        Assert.Equal(43, Assert.Single(step.Resources));
        Assert.Empty(step.Targets);
        var world = game.State;
        Assert.Equal("01087", world.Cards[41].FaceId);
        Assert.Equal("01089", world.Cards[43].FaceId);
        Assert.Equal(96, Assert.Single(Statuses.On(world, world.Seats[0].IdentityCard, Statuses.Stunned)).ObjectId);
        var health = world.Cards.Select(card => card.Tokens.GetValueOrDefault("health")).ToArray();
        long rng = world.Random.Generator.WordsConsumed;
        // "Status card abilities have timing priority over all conflicting triggered abilities."
        // "Costs associated with the attack attempt ... must still be paid."
        // The engine chooses to settle status replacement before an effect-target prompt.
        var result = game.Resolve(DecisionFor(game, step).Resolve(game.Pending!));
        Assert.Equal(3, step.Events.Count);
        Assert.Equal(6, result.Events.Count);
        JournalReplay.RequireEvents(step.Events, result.Events.Take(3).ToList(), "paid Haymaker");
        JournalReplay.RequireEvents(ReadStep(lines, 17).Events, result.Events.Skip(3).ToList(), "stun cancellation");
        var removed = Assert.IsType<CardsMoved>(result.Events[3]);
        Assert.Equal("StatusArea", removed.From.Zone);
        Assert.Equal("RemovedArea", removed.To.Zone);
        Assert.Equal(96, Assert.Single(removed.Cards).Card);
        var detached = Assert.IsType<CardDetached>(result.Events[4]);
        Assert.Equal(96, detached.Card);
        Assert.Equal(1, detached.Host);
        // "That character is not considered to have attacked."
        Assert.Equal(health, world.Cards.Select(card => card.Tokens.GetValueOrDefault("health")));
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, world.Cards[41].Area.Type);
        Assert.Equal(DeckType.DiscardPile, world.Cards[43].Area.Type);
        Assert.Equal("WhenPlayerInTurn", game.Pending!.Trigger);
        Assert.Equal("01087: choose a card", ReadStep(lines, 17).Prompt.Label);
        var failure = Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequireEvents(step.Events, result.Events, "step 16 events"));
        Assert.Equal("step 16 events diverged: expected 3, got 6", failure.Message);

        // Synthetic causal control: remove only stun from an independently verified prefix.
        // This board identifies the cause; it is not a played game or accepted old replay.
        var (_, control) = OpenRecordedGame();
        ReplayPrefix(lines, control, 16);
        World.MoveToTop(control.State.Cards[96], control.State.AreaOf(DeckType.RemovedArea));
        var continued = control.Resolve(DecisionFor(control, step).Resolve(control.Pending!));
        JournalReplay.RequireEvents(step.Events, continued.Events, "unstunned payment");
        JournalReplay.RequirePrompt(ReadStep(lines, 17).Prompt, control.Pending!, "unstunned target");
    }

    [Fact]
    public void CurrentLegalSupportAndAllyThwartsCompleteWithExactResultsAndReplay()
    {
        var (lines, game) = OpenRecordedGame();
        var journal = new List<JournalStep>();
        // The current played route omits the obsolete effect-target answer at old step 17.
        // Every submitted answer must match its current prompt before it resolves.
        foreach (int index in Enumerable.Range(0, 89).Where(index => index != 17))
        {
            var source = ReadStep(lines, index);
            var prompt = game.Pending!;
            JournalReplay.RequirePrompt(source.Prompt, prompt, $"current source {index}");
            var durable = DecisionFor(game, source);
            var result = game.Resolve(durable.Resolve(prompt));
            var expectedEvents = index == 16
                ? source.Events.Concat(ReadStep(lines, 17).Events).ToList() : source.Events;
            JournalReplay.RequireEvents(expectedEvents, result.Events, $"current source {index}");
            JournalReplay.RequireFingerprint(ReadStep(lines, index == 16 ? 17 : index).Digest,
                game.State.Digest().Fingerprint(), $"current source {index}");
            journal.Add(new JournalStep(PromptRecord.From(prompt), durable,
                result.Events.Select(JournalJson.Event).ToList(),
                game.State.Random.Generator.WordsConsumed, game.State.Digest().Fingerprint()));
            if (index == 87)
            {
                Assert.Equal("01064: choose a card", prompt.Label);
                Assert.Equal("04070", source.Decision.Label);
                Assert.Equal(1, game.State.Cards[63].Tokens["k_threat"]);
            }
            if (index == 88) AssertAllyDefeat(game, source, result.Events);
        }
        Assert.Equal(88, journal.Count);
        var terminal = SimulationRecordContract.Read<ResultRecord>(lines[91], "result");
        Assert.Equal(89, terminal.Decisions);
        Assert.Equal("VillainWins", terminal.Outcome);
        Assert.Equal(terminal.Outcome, game.State.Result.ToString());
        Assert.Equal(terminal.TerminalDigest, game.State.Digest().Canonical());
        Assert.Null(game.Pending);

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
        Assert.Equal(terminal.TerminalDigest, replay.State.Digest().Canonical());
    }

    private static void AssertAllyDefeat(Game game, StepRecord step, IReadOnlyList<GameEvent> events)
    {
        Assert.Equal("Thwart", step.Decision.Verb);
        Assert.Equal(8, step.Decision.AnchorId);
        Assert.Equal(63, Assert.Single(step.Targets));
        Assert.Equal("01002", game.State.Cards[8].FaceId);
        Assert.Equal("04070", game.State.Cards[63].FaceId);
        var thwart = Assert.IsType<FieldSet>(events[1]);
        Assert.Equal(63, thwart.Card);
        Assert.Equal("k_threat", thwart.Field);
        Assert.Equal(1, thwart.From);
        Assert.Equal(0, thwart.To);
        Assert.Equal(8, thwart.Cause!.Source);
        Assert.Equal(63, thwart.Cause.Target);
        var boost = Assert.IsType<BoostResolved>(events[4]);
        Assert.Equal(52, boost.Enemy);
        Assert.False(boost.Attacking);
        Assert.Equal(1, boost.Cause!.Target);
        var threat = Assert.IsType<FieldSet>(events[6]);
        Assert.Equal(50, threat.Card);
        Assert.Equal("k_threat", threat.Field);
        Assert.Equal(4, threat.From);
        Assert.Equal(8, threat.To);
        Assert.Equal(1, game.State.Cards[50].Tokens["is_completed"]);
    }

    private static void ReplayPrefix(string[] lines, Game game, int count)
    {
        for (int index = 0; index < count; index++)
        {
            var step = ReadStep(lines, index);
            Assert.Equal(index, step.Step);
            JournalReplay.RequirePrompt(step.Prompt, game.Pending!, $"step {index}");
            var result = game.Resolve(DecisionFor(game, step).Resolve(game.Pending!));
            JournalReplay.RequireEvents(step.Events, result.Events, $"step {index}");
            JournalReplay.RequireFingerprint(step.Digest, game.State.Digest().Fingerprint(), $"step {index}");
        }
    }

    private static StepRecord ReadStep(string[] lines, int index) =>
        SimulationRecordContract.Read<StepRecord>(lines[index + 2], "step");

    private static DurableDecision DecisionFor(Game game, StepRecord step) => new(
        DurableDecision.SimulationActor(game.Pending!, step.Decision), step.Decision,
        step.Targets, step.Resources, step.Values, step.Allocations);

    private static (string[] Lines, Game Game) OpenRecordedGame()
    {
        string path = Path.Combine(RepositoryRoot(), "tests", "Marvel.Acceptance.Tests",
            "Fixtures", "scheme-defeat-13.jsonl.gz");
        Assert.Equal("2BC9FEAF2A6E11B9ADA12343C09FCEFDACB029A85A10E2BADFB89FB0DC339F55",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        string[] lines = [.. SimulationRecordFiles.ReadLines(path)];
        Assert.Equal(93, lines.Length);
        var header = SimulationRecordContract.Read<HeaderRecord>(lines[0], "header");
        var start = SimulationRecordContract.Read<StartRecord>(lines[1], "start");
        Assert.Equal("campaign-research", header.Policy);
        Assert.Equal(279u, header.PolicySeed);
        Assert.Equal(13u, start.Seed);
        var config = new SimulationConfig(header.Scenario, header.Difficulty, header.Heroes,
            header.ModularSets, 1, [start.Seed], null, null, header.PolicySeed,
            header.DecisionLimit, null, RepositoryRoot());
        var opened = SimulationHarnessSupport.Open(SimulationData.Load(RepositoryRoot()), config, start.Seed);
        Assert.Equal(start.InitialDigest, opened.Game.State.Digest().Canonical());
        JournalReplay.RequireEvents(start.SetupEvents, opened.SetupEvents, "setup");
        return (lines, opened.Game);
    }
}
