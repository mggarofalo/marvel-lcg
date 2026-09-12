using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using static Marvel.Sim.SimulationHarnessSupport;

namespace Marvel.Sim;

internal sealed class SimulationReplaySession
{
    private readonly ReplayConfig config;
    private readonly TextWriter diagnostics;
    private HeaderRecord header = null!;
    private SimulationData data = null!;
    private SimulationConfig simulation = null!;
    private Game? game;
    private ActingPolicy? policy;
    private int currentGame = -1, currentSteps, games, steps;
    private int playerWins, villainWins, playerLosses, failures, rounds;
    private int cardsPlayed, playerAttacks, payments, resourceAbilities;
    private readonly Dictionary<string, int> signatures = new(StringComparer.Ordinal);
    private readonly Queue<StepRecord> recent = new();
    private bool sawSummary;

    internal SimulationReplaySession(ReplayConfig config, TextWriter diagnostics)
    {
        this.config = config;
        this.diagnostics = diagnostics;
    }

    internal ReplaySummary Replay()
    {
        using IEnumerator<string> lines = SimulationRecordFiles
            .ReadLines(Path.GetFullPath(config.Path)).GetEnumerator();
        ReadLeadingHeader(lines);
        while (lines.MoveNext()) Process(lines.Current);
        return Finish();
    }

    private void ReadLeadingHeader(IEnumerator<string> lines)
    {
        if (!lines.MoveNext()) throw new SimulationUsageException("record is empty");
        using (var document = JsonDocument.Parse(lines.Current))
            RequireEqual("header", RecordType(document.RootElement), "first record type");
        header = Read<HeaderRecord>(lines.Current, "header");
        ValidateRecordSchema(header.Schema);
        ValidatePolicy();
        ValidateHeaderSeeds(header);
        data = SimulationData.Load(config.RepoRoot);
        simulation = new SimulationConfig(
            header.Scenario, header.Difficulty, header.Heroes, header.ModularSets,
            1, [], null, header.SelectionSeed, 0, header.DecisionLimit, null, config.RepoRoot);
        Validate(data.Setup, simulation);
    }

    private void ValidatePolicy()
    {
        bool nameMatches = string.Equals(header.Policy, ActingPolicy.Name,
            StringComparison.Ordinal);
        if (!nameMatches || header.PolicyVersion != ActingPolicy.Version)
            throw new SimulationUsageException(
                $"policy {header.Policy} v{header.PolicyVersion} is not available");
        RequireEqual(ActingPolicy.Visibility, header.PolicyVisibility, "policy visibility");
    }

    private void Process(string line)
    {
        if (sawSummary)
            throw new ReplayDivergenceException("a record appeared after the terminal summary");
        using var document = JsonDocument.Parse(line);
        string type = RecordType(document.RootElement);
        switch (type)
        {
            case "start": Start(Read<StartRecord>(line, type)); break;
            case "step": Resolve(Read<StepRecord>(line, type, header.Schema)); break;
            case "result": Complete(Read<ResultRecord>(line, type)); break;
            case "failure": new SimulationFailureReplay(this,
                Read<FailureRecord>(line, type, header.Schema)).Reproduce(); break;
            case "summary": VerifySummary(Read<SummaryRecord>(line, type)); break;
            default: throw new SimulationUsageException($"unknown record type '{type}'");
        }
    }

    private void Start(StartRecord start)
    {
        if (game is not null)
            throw new ReplayDivergenceException(
                $"game {start.Game} started before game {currentGame} ended");
        RequireEqual(games, start.Game, "start game index");
        ValidateStartSeed(start);
        var opened = Open(data, simulation, start.Seed);
        game = opened.Game;
        policy = new ActingPolicy(data.Cards, start.SeatPolicySeeds);
        currentGame = start.Game;
        currentSteps = 0;
        recent.Clear();
        games++;
        RequireEqual(start.InitialDigest, game.State.Digest().Canonical(),
            $"game {currentGame} initial digest");
        RequireSequence(start.SeatPolicySeeds,
            SeatPolicySeeds(start.PolicySeed, header.Heroes.Count),
            $"game {currentGame} seat policy seeds");
        JournalReplay.RequireEvents(start.SetupEvents, opened.SetupEvents,
            $"game {currentGame} setup events");
    }

    private void ValidateStartSeed(StartRecord start)
    {
        if (start.Game < 0 || start.Game >= header.Seeds.Count)
            throw new ReplayDivergenceException(
                $"start names game index {start.Game} outside the seed plan");
        RequireEqual(header.Seeds[start.Game], start.Seed,
            $"game {start.Game} planned seed");
        RequireEqual(unchecked(header.PolicySeed + (uint)start.Game), start.PolicySeed,
            $"game {start.Game} policy master seed");
    }

    private void Resolve(StepRecord step)
    {
        RequireGame(game, currentGame, step.Game);
        RequireEqual(currentSteps, step.Step, $"game {currentGame} step index");
        var asked = game!.Pending ?? throw new ReplayDivergenceException(
            $"game {currentGame} ended before recorded step {step.Step}");
        JournalReplay.RequirePrompt(step.Prompt, asked,
            $"game {currentGame} step {step.Step} prompt");
        var policyDecision = policy!.Answer(game);
        VerifyPolicyDecision(step, asked, policyDecision);
        var decision = new DurableDecision(
            DurableDecision.SimulationActor(asked, step.Decision), step.Decision,
            step.Targets, step.Resources, step.Values, step.Allocations).Resolve(asked);
        var resolved = game.Resolve(decision);
        policy.DecisionResolved();
        recent.Enqueue(step);
        if (recent.Count > RecentEventLimit) recent.Dequeue();
        JournalReplay.RequireEvents(step.Events, resolved.Events,
            $"game {currentGame} step {step.Step} events");
        JournalReplay.RequireFingerprint(step.Digest, game.State.Digest().Fingerprint(),
            $"game {currentGame} step {step.Step} digest");
        steps++;
        currentSteps++;
    }

    private void VerifyPolicyDecision(StepRecord step, Prompt asked, Decision decision)
    {
        RequireEqual(Json(step.Decision), Json(DecisionSelector.From(asked, decision)),
            $"game {currentGame} step {step.Step} policy decision");
        RequireSequence(step.Targets, decision.Targets,
            $"game {currentGame} step {step.Step} policy targets");
        RequireSequence(step.Resources, decision.Spent,
            $"game {currentGame} step {step.Step} policy resources");
        RequireEqual(Json(step.Values), Json(decision.DefinedValues),
            $"game {currentGame} step {step.Step} policy variables");
    }

    private void Complete(ResultRecord result)
    {
        RequireGame(game, currentGame, result.Game);
        RequireEqual(currentSteps, result.Decisions, $"game {currentGame} decision count");
        RequireEqual(header.Seeds[result.Game], result.Seed, $"game {currentGame} result seed");
        RequireEqual(result.Outcome, game!.State.Result.ToString(), $"game {currentGame} outcome");
        RequireEqual(result.Round, game.Round, $"game {currentGame} round");
        RequireEqual(result.TerminalDigest, game.State.Digest().Canonical(),
            $"game {currentGame} terminal digest");
        RequireEqual(Json(result.Metrics), Json(policy!.Metrics),
            $"game {currentGame} policy metrics");
        if (game.Pending is not null)
            throw new ReplayDivergenceException($"game {currentGame} has a prompt after its result");
        AddOutcome(game.State.Result);
        AddCompletedMetrics();
        CloseGame();
    }

    private void AddOutcome(Outcome outcome)
    {
        if (outcome == Outcome.PlayersWin) playerWins++;
        else if (outcome == Outcome.VillainWins) villainWins++;
        else if (outcome == Outcome.PlayersLose) playerLosses++;
    }

    private void VerifySummary(SummaryRecord summary)
    {
        if (game is not null)
            throw new ReplayDivergenceException(
                $"summary appeared while game {currentGame} was active");
        RequireEqual(games, summary.Games, "summary game count");
        RequireEqual(playerWins, summary.PlayersWin, "summary player wins");
        RequireEqual(villainWins, summary.VillainWins, "summary villain wins");
        RequireEqual(playerLosses, summary.PlayersLose, "summary player losses");
        RequireEqual(failures, summary.Failures, "summary failures");
        RequireEqual(steps, summary.Decisions, "summary decisions");
        RequireEqual(rounds, summary.Rounds, "summary rounds");
        RequireEqual(cardsPlayed, summary.CardsPlayed, "summary cards played");
        RequireEqual(playerAttacks, summary.PlayerAttacks, "summary player attacks");
        RequireEqual(payments, summary.Payments, "summary payments");
        RequireEqual(resourceAbilities, summary.ResourceAbilitiesUsed,
            "summary resource abilities");
        RequireEqual(Json(signatures), Json(summary.FailureSignatures),
            "summary failure signatures");
        sawSummary = true;
    }

    private ReplaySummary Finish()
    {
        if (game is not null)
            throw new ReplayDivergenceException(
                $"record ended while game {currentGame} was still open");
        RequireEqual(header.Seeds.Count, games, "recorded game count");
        if (!sawSummary)
            throw new ReplayDivergenceException("record has no terminal summary");
        return new ReplaySummary(games, steps);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, RecordJson.Options);
    internal HeaderRecord Header => header;
    internal SimulationData Data => data;
    internal SimulationConfig Simulation => simulation;
    internal TextWriter Diagnostics => diagnostics;
    internal Game? Game => game;
    internal ActingPolicy? Policy => policy;
    internal int CurrentGame => currentGame;
    internal int CurrentSteps => currentSteps;
    internal Queue<StepRecord> Recent => recent;

    internal void CountSetupFailure(FailureRecord failure)
    {
        AddFailure(failure, signatures, ref failures);
        games++;
        currentGame = failure.Game;
    }

    internal void CountActiveFailure(FailureRecord failure)
    {
        AddFailure(failure, signatures, ref failures);
        AddCompletedMetrics();
    }

    private void AddCompletedMetrics()
    {
        rounds += game!.Round;
        AddMetrics(policy!.Metrics, ref cardsPlayed, ref playerAttacks,
            ref payments, ref resourceAbilities);
    }

    internal void CloseGame()
    {
        game = null;
        policy = null;
    }
}
