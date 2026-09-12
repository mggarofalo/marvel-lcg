using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;

using static Marvel.Sim.SimulationRunHarness;
using static Marvel.Sim.SimulationReplayHarness;
using static Marvel.Sim.SimulationReportReader;
using static Marvel.Sim.SimulationHarnessSupport;
namespace Marvel.Sim;

internal static class SimulationRunHarness
{
    public static SimulationSummary Run(
        SimulationConfig config, TextWriter records, TextWriter diagnostics)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new SimulationRunSession(config, records, diagnostics).Run();
    }
}

internal sealed class SimulationRunSession(
    SimulationConfig config, TextWriter records, TextWriter diagnostics)
{
    private readonly SimulationData data = SimulationData.Load(config.RepoRoot);
    private readonly SimulationRunTotals totals = new();

    internal SimulationSummary Run()
    {
        Validate(data.Setup, config);
        var seeds = PlanSeeds(config);
        WriteHeader(seeds);
        for (int index = 0; index < seeds.Count; index++)
        {
            new SimulationGameRun(config, data, records, diagnostics, totals,
                index, seeds[index]).Run();
        }
        return WriteSummary(seeds.Count);
    }

    private void WriteHeader(IReadOnlyList<uint> seeds)
    {
        string seedMode = config.ExplicitSeeds.Count > 0 ? "explicit"
            : config.SelectionSeed.HasValue ? "random" : "consecutive";
        diagnostics.WriteLine($"selected seeds: {string.Join(',', seeds)}");
        RecordJson.Write(records, new HeaderRecord(
            "header", RecordSchema, config.Scenario, config.Difficulty, config.Heroes,
            config.ModularSets, ActingPolicy.Name, ActingPolicy.Version,
            ActingPolicy.Visibility, config.PolicySeed, config.DecisionLimit,
            seedMode, config.SelectionSeed, seeds));
    }

    private SimulationSummary WriteSummary(int games)
    {
        var summary = totals.Summary(games);
        RecordJson.Write(records, new SummaryRecord(
            "summary", summary.Games, summary.PlayersWin, summary.VillainWins,
            summary.PlayersLose, summary.Failures, summary.Decisions,
            summary.Rounds, summary.CardsPlayed, summary.PlayerAttacks,
            summary.Payments, summary.ResourceAbilitiesUsed, summary.FailureSignatures));
        return summary;
    }
}

internal sealed class SimulationGameRun(
    SimulationConfig config, SimulationData data, TextWriter records,
    TextWriter diagnostics, SimulationRunTotals totals, int gameIndex, uint seed)
{
    private readonly uint policySeed = unchecked(config.PolicySeed + (uint)gameIndex);
    private Game? game;
    private ActingPolicy? policy;
    private DecisionSelector? attempted;
    private Decision? input;
    private PromptRecord? prompt;
    private string? lastGood;
    private readonly Queue<StepRecord> recent = new();
    private int step;
    private string stage = "setup";

    internal void Run()
    {
        try { Play(); }
        catch (Exception error) when (error is not SimulationUsageException)
        {
            RecordFailure(error);
        }
        finally { totals.Add(game, policy); }
    }

    private void Play()
    {
        var policySeeds = SeatPolicySeeds(policySeed, config.Heroes.Count);
        var opened = Open(data, config, seed);
        game = opened.Game;
        lastGood = game.State.Digest().Canonical();
        RecordJson.Write(records, new StartRecord(
            "start", gameIndex, seed, policySeed, policySeeds, lastGood,
            [.. opened.SetupEvents.Select(JournalJson.Event)]));
        policy = new ActingPolicy(data.Cards, policySeeds);
        while (game.Pending is not null) ResolveStep();
        RecordResult();
    }

    private void ResolveStep()
    {
        stage = "policy";
        if (step >= config.DecisionLimit)
        {
            throw new SimulationRunException(
                $"decision limit {config.DecisionLimit} reached at '{game!.Pending!.Label}'");
        }
        var asked = game!.Pending!;
        input = policy!.Answer(game);
        var durable = DurableDecision.From(
            DurableDecision.SimulationActor(asked, input), asked, input);
        prompt = PromptRecord.From(asked);
        attempted = durable.Selector;
        stage = "resolve";
        var resolved = game.Resolve(durable.Resolve(asked));
        policy.DecisionResolved();
        lastGood = game.State.Digest().Canonical();
        var journal = new JournalStep(prompt, durable,
            [.. resolved.Events.Select(JournalJson.Event)],
            game.State.Random.Generator.WordsConsumed, game.State.Digest().Fingerprint());
        var recorded = new StepRecord(
            "step", gameIndex, step, journal.Prompt, journal.Decision.Selector,
            journal.Decision.Targets, journal.Decision.Resources, journal.Decision.Values,
            journal.Decision.Allocations, journal.Events, journal.StateFingerprint);
        recent.Enqueue(recorded);
        if (recent.Count > RecentEventLimit) recent.Dequeue();
        RecordJson.Write(records, recorded);
        step++;
        totals.Decisions++;
        attempted = null;
        input = null;
    }

    private void RecordResult()
    {
        stage = "terminal";
        string outcome = game!.State.Result.ToString();
        totals.AddOutcome(game.State.Result, outcome);
        RecordJson.Write(records, new ResultRecord(
            "result", gameIndex, seed, outcome, game.Round, step,
            policy!.Metrics, game.State.Digest().Canonical()));
    }

    private void RecordFailure(Exception error)
    {
        totals.AddFailure(error);
        string signature = $"{error.GetType().Name}: {error.Message}";
        diagnostics.WriteLine($"game {gameIndex}, seed {seed}: {signature}");
        int round = game is null ? 0 : game.Round;
        PolicyMetrics metrics = policy is null
            ? new PolicyMetrics(0, 0, 0, 0) : policy.Metrics;
        string exceptionName = error.GetType().FullName ?? error.GetType().Name;
        var targets = input is null ? [] : input.Targets;
        var spent = input is null ? [] : input.Spent;
        var values = input is null
            ? new Dictionary<string, long>(StringComparer.Ordinal) : input.DefinedValues;
        var allocations = input is null ? [] : input.Allocated;
        string? postFailure = game is null ? null : game.State.Digest().Canonical();
        RecordJson.Write(records, new FailureRecord(
            "failure", FailureCategory(error, stage), gameIndex, seed, step,
            round, metrics, exceptionName, error.Message,
            FailurePrompt(stage, attempted, prompt, game), attempted,
            targets, spent, values, allocations, lastGood, postFailure,
            [.. recent], Reproduce(config, seed, policySeed)));
    }
}

internal sealed class SimulationRunTotals
{
    private int playerWins, villainWins, playerLosses, failures;
    private int rounds, cardsPlayed, playerAttacks, payments, resourceAbilities;
    private readonly Dictionary<string, int> signatures = new(StringComparer.Ordinal);
    internal int Decisions { get; set; }

    internal void AddOutcome(Outcome outcome, string name)
    {
        if (outcome == Outcome.PlayersWin) playerWins++;
        else if (outcome == Outcome.VillainWins) villainWins++;
        else if (outcome == Outcome.PlayersLose) playerLosses++;
        else throw new SimulationRunException($"game ended without a terminal outcome: {name}");
    }

    internal void AddFailure(Exception error)
    {
        failures++;
        string signature = $"{error.GetType().Name}: {error.Message}";
        signatures[signature] = signatures.GetValueOrDefault(signature) + 1;
    }

    internal void Add(Game? game, ActingPolicy? policy)
    {
        if (game is not null) rounds += game.Round;
        if (policy is null) return;
        cardsPlayed += policy.CardsPlayed;
        playerAttacks += policy.PlayerAttacks;
        payments += policy.Payments;
        resourceAbilities += policy.ResourceAbilitiesUsed;
    }

    internal SimulationSummary Summary(int games) => new(
        games, playerWins, villainWins, playerLosses, failures, Decisions, rounds,
        cardsPlayed, playerAttacks, payments, resourceAbilities, signatures);
}
