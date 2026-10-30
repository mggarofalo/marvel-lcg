using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
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
        FrozenPromptCompatibility.Require(step.Prompt, game.Pending!, game.State, "step 16");
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
        FrozenPromptCompatibility.Require(ReadStep(lines, 17).Prompt, control.Pending!, control.State, "unstunned target");
    }

    [Fact]
    [Rule("rr:you-your.7")]
    [Rule("rr:forced.1")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:indirect-damage.1")]
    public void CurrentLegalGameMeetsTheMachineGunAtItsFirstAttackAndReplays()
    {
        var (lines, game) = OpenRecordedGame();
        var journal = new CurrentLegalJournal();
        // The current played route omits the obsolete effect-target answer at old step 17.
        // Every submitted answer must match its current prompt before it resolves.
        foreach (int index in Enumerable.Range(0, 46).Where(index => index != 17))
        {
            var source = ReadStep(lines, index);
            var prompt = game.Pending!;
            FrozenPromptCompatibility.Require(source.Prompt, prompt, game.State, $"current source {index}");
            var durable = DecisionFor(game, source);
            var result = game.Resolve(durable.Resolve(prompt));
            journal.Record(game, prompt, durable, result.Events);
            if (index == 45)
            {
                AssertMachineGunFires(game, source, result.Events);
                break;
            }
            var expectedEvents = index == 16
                ? source.Events.Concat(ReadStep(lines, 17).Events).ToList() : source.Events;
            JournalReplay.RequireEvents(expectedEvents, result.Events, $"current source {index}");
            JournalReplay.RequireFingerprint(ReadStep(lines, index == 16 ? 17 : index).Digest,
                game.State.Digest().Fingerprint(), $"current source {index}");
        }
        Assert.Equal(45, journal.Count);

        // The discarded card shifts every later encounter card, so the recorded
        // intentions stop applying. The acting policy completes the current game.
        var header = SimulationRecordContract.Read<HeaderRecord>(lines[0], "header");
        journal.CompleteWithActingPolicy(game, RepositoryRoot(), header.PolicySeed, header.DecisionLimit);
        Assert.Equal(71, journal.Count);
        Assert.Equal(Outcome.PlayersLose, game.State.Result);
        string terminal = game.State.Digest().Canonical();

        var (_, replay) = OpenRecordedGame();
        journal.Replay(replay);
        Assert.Equal(game.State.Result, replay.State.Result);
        Assert.Equal(terminal, replay.State.Digest().Canonical());
    }

    [Fact]
    public void PreCorrectionMachineGunRowReproducesTheFrozenAllyThwartGame()
    {
        // Causal control, not a current game: only the 04064 trigger reverts to the
        // scenario-controller binding that never matched. Every recorded fact after
        // the obsolete step-17 answer, including the ally thwart at 88 and the
        // terminal digest, then reproduces exactly under the current engine.
        var (lines, game) = OpenRecordedGame(PreCorrectionMachineGun);
        foreach (int index in Enumerable.Range(0, 89).Where(index => index != 17))
        {
            var source = ReadStep(lines, index);
            var prompt = game.Pending!;
            FrozenPromptCompatibility.Require(source.Prompt, prompt, game.State, $"control source {index}");
            var result = game.Resolve(DecisionFor(game, source).Resolve(prompt));
            var expectedEvents = index == 16
                ? source.Events.Concat(ReadStep(lines, 17).Events).ToList() : source.Events;
            JournalReplay.RequireEvents(expectedEvents, result.Events, $"control source {index}");
            JournalReplay.RequireFingerprint(ReadStep(lines, index == 16 ? 17 : index).Digest,
                game.State.Digest().Fingerprint(), $"control source {index}");
            if (index == 87)
            {
                Assert.Equal("01064: choose a card", prompt.Label);
                Assert.Equal("04070", source.Decision.Label);
                Assert.Equal(1, game.State.Cards[63].Tokens["k_threat"]);
            }
            if (index == 88) AssertAllyDefeat(game, source, result.Events);
        }
        var terminal = SimulationRecordContract.Read<ResultRecord>(lines[91], "result");
        Assert.Equal(89, terminal.Decisions);
        Assert.Equal("VillainWins", terminal.Outcome);
        Assert.Equal(terminal.Outcome, game.State.Result.ToString());
        Assert.Equal(terminal.TerminalDigest, game.State.Digest().Canonical());
        Assert.Null(game.Pending);
    }

    private static void AssertMachineGunFires(Game game, StepRecord step, IReadOnlyList<GameEvent> events)
    {
        // Crossbones II's reveal attached the gun at 43; this is his first attack since.
        // The recorded nine events stay exact through his attack initiation.
        Assert.Equal(9, step.Events.Count);
        Assert.Equal(14, events.Count);
        JournalReplay.RequireEvents(step.Events, events.Take(9).ToList(), "current source 45 prefix");
        var attack = Assert.IsType<AttackBoundary>(events[8]);
        Assert.Equal(52, attack.Source);
        Assert.Equal(1, attack.Target);
        Assert.Equal("04064", game.State.Cards[53].FaceId);
        // "you" is the attacked player (rr:you-your.7). The forced interrupt pays its
        // ammo and encounter-discard costs before the boost card is dealt.
        var ammo = Assert.IsType<FieldSet>(events[9]);
        Assert.Equal((53, "c_ammo", 2, 1), (ammo.Card, ammo.Field, ammo.From, ammo.To));
        var discarded = Assert.IsType<CardsMoved>(events[10]);
        Assert.Equal("EncounterDeck", discarded.From.Zone);
        Assert.Equal("EncounterDiscardPile", discarded.To.Zone);
        Assert.Equal(57, Assert.Single(discarded.Cards).Card);
        Assert.Equal("04067", game.State.Cards[57].FaceId);
        Assert.IsType<ActivityCompleted>(events[11]);
        // Spider-Man controls no other character, so both boost icons' indirect damage is his.
        var damage = Assert.IsType<FieldSet>(events[12]);
        Assert.Equal((1, "health", 8, 6), (damage.Card, damage.Field, damage.From, damage.To));
        Assert.Equal(53, Assert.IsType<AbilityWindowResult>(events[13]).Source);
        Assert.Equal(1, game.State.Cards[53].Tokens["c_ammo"]);
        var failure = Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequireEvents(step.Events, events, "step 45 events"));
        Assert.Equal("step 45 events diverged: expected 9, got 14", failure.Message);
    }

    private static SimulationData PreCorrectionMachineGun(SimulationData data)
    {
        var book = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "datasets", "abilities", "abilities.json")))!;
        var trigger = book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "04064")!
            ["abilities"]![0]!["trigger"]!;
        Assert.Equal("trigger.player", trigger["player"]!.GetValue<string>());
        trigger["player"] = "you";
        return data with { Abilities = AbilityCatalog.Parse(book.ToJsonString()) };
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
            FrozenPromptCompatibility.Require(step.Prompt, game.Pending!, game.State, $"step {index}");
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

    private static (string[] Lines, Game Game) OpenRecordedGame(
        Func<SimulationData, SimulationData>? content = null)
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
        var data = SimulationData.Load(RepositoryRoot());
        var opened = SimulationHarnessSupport.Open(content is null ? data : content(data), config, start.Seed);
        Assert.Equal(start.InitialDigest, opened.Game.State.Digest().Canonical());
        JournalReplay.RequireEvents(start.SetupEvents, opened.SetupEvents, "setup");
        return (lines, opened.Game);
    }
}
