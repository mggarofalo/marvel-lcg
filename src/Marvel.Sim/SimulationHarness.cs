using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Sim;

internal static class SimulationHarness
{
    private const int RecordSchema = 2;
    private const int RecentEventLimit = 20;

    public static void ValidateConfig(SimulationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var data = SimulationData.Load(config.RepoRoot);
        Validate(data.Setup, config);
        _ = PlanSeeds(config);
    }

    public static SimulationSummary Run(
        SimulationConfig config, TextWriter records, TextWriter diagnostics)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var data = SimulationData.Load(config.RepoRoot);
        Validate(data.Setup, config);
        var seeds = PlanSeeds(config);
        string seedMode = config.ExplicitSeeds.Count > 0 ? "explicit"
            : config.SelectionSeed.HasValue ? "random"
            : "consecutive";
        diagnostics.WriteLine($"selected seeds: {string.Join(',', seeds)}");
        RecordJson.Write(records, new HeaderRecord(
            "header", RecordSchema, config.Scenario, config.Difficulty, config.Heroes,
            config.ModularSets, ActingPolicy.Name, ActingPolicy.Version,
            ActingPolicy.Visibility, config.PolicySeed, config.DecisionLimit,
            seedMode, config.SelectionSeed, seeds));

        int playerWins = 0;
        int villainWins = 0;
        int playerLosses = 0;
        int failures = 0;
        int decisions = 0;
        int rounds = 0;
        int cardsPlayed = 0;
        int playerAttacks = 0;
        int payments = 0;
        int resourceAbilities = 0;
        var signatures = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int gameIndex = 0; gameIndex < seeds.Count; gameIndex++)
        {
            uint seed = seeds[gameIndex];
            uint policySeed = unchecked(config.PolicySeed + (uint)gameIndex);
            var policySeeds = SeatPolicySeeds(policySeed, config.Heroes.Count);
            Game? game = null;
            ActingPolicy? policy = null;
            DecisionSelector? attempted = null;
            Decision? input = null;
            PromptRecord? prompt = null;
            string? lastGood = null;
            var recent = new Queue<StepRecord>();
            int step = 0;
            string stage = "setup";
            try
            {
                var opened = Open(data, config, seed);
                game = opened.Game;
                lastGood = game.State.Digest().Canonical();
                RecordJson.Write(records, new StartRecord(
                    "start", gameIndex, seed, policySeed, policySeeds, lastGood,
                    [.. opened.SetupEvents.Select(RecordJson.Event)]));

                policy = new ActingPolicy(data.Cards, policySeeds);
                while (game.Pending is not null)
                {
                    stage = "policy";
                    if (step >= config.DecisionLimit)
                    {
                        throw new SimulationRunException(
                            $"decision limit {config.DecisionLimit} reached at "
                            + $"'{game.Pending.Label}'");
                    }

                    var asked = game.Pending;
                    prompt = PromptRecord.From(asked);
                    input = policy.Answer(game);
                    attempted = DecisionSelector.From(asked, input);
                    stage = "resolve";
                    var resolved = game.Resolve(input);
                    policy.DecisionResolved();
                    var happened = resolved.Events.Select(RecordJson.Event).ToList();
                    lastGood = game.State.Digest().Canonical();
                    var recordedStep = new StepRecord(
                        "step", gameIndex, step, prompt, attempted,
                        input.Targets,
                        input.Spent,
                        input.DefinedValues,
                        happened,
                        game.State.Digest().Fingerprint());
                    recent.Enqueue(recordedStep);
                    while (recent.Count > RecentEventLimit)
                    {
                        recent.Dequeue();
                    }

                    RecordJson.Write(records, recordedStep);
                    step++;
                    decisions++;
                    attempted = null;
                    input = null;
                }

                stage = "terminal";
                string outcome = game.State.Result.ToString();
                switch (game.State.Result)
                {
                    case Outcome.PlayersWin:
                        playerWins++;
                        break;
                    case Outcome.VillainWins:
                        villainWins++;
                        break;
                    case Outcome.PlayersLose:
                        playerLosses++;
                        break;
                    default:
                        throw new SimulationRunException(
                            $"game ended without a terminal outcome: {outcome}");
                }

                RecordJson.Write(records, new ResultRecord(
                    "result", gameIndex, seed, outcome, game.Round, step,
                    policy.Metrics, game.State.Digest().Canonical()));
            }
            catch (Exception error) when (error is not SimulationUsageException)
            {
                failures++;
                string signature = $"{error.GetType().Name}: {error.Message}";
                signatures[signature] = signatures.GetValueOrDefault(signature) + 1;
                diagnostics.WriteLine($"game {gameIndex}, seed {seed}: {signature}");
                RecordJson.Write(records, new FailureRecord(
                    "failure", FailureCategory(error, stage), gameIndex, seed, step,
                    game?.Round ?? 0,
                    policy?.Metrics ?? new PolicyMetrics(0, 0, 0, 0),
                    error.GetType().FullName ?? error.GetType().Name,
                    error.Message,
                    FailurePrompt(stage, attempted, prompt, game),
                    attempted,
                    input?.Targets ?? [],
                    input?.Spent ?? [],
                    input?.DefinedValues
                        ?? new Dictionary<string, long>(StringComparer.Ordinal),
                    lastGood,
                    game?.State.Digest().Canonical(),
                    [.. recent],
                    Reproduce(config, seed, policySeed)));
            }
            finally
            {
                rounds += game?.Round ?? 0;
                cardsPlayed += policy?.CardsPlayed ?? 0;
                playerAttacks += policy?.PlayerAttacks ?? 0;
                payments += policy?.Payments ?? 0;
                resourceAbilities += policy?.ResourceAbilitiesUsed ?? 0;
            }
        }

        var summary = new SimulationSummary(
            seeds.Count, playerWins, villainWins, playerLosses, failures,
            decisions, rounds, cardsPlayed, playerAttacks, payments,
            resourceAbilities, signatures);
        RecordJson.Write(records, new SummaryRecord(
            "summary", summary.Games, summary.PlayersWin, summary.VillainWins,
            summary.PlayersLose, summary.Failures, summary.Decisions,
            summary.Rounds, summary.CardsPlayed, summary.PlayerAttacks,
            summary.Payments, summary.ResourceAbilitiesUsed,
            summary.FailureSignatures));
        return summary;
    }

    public static ReplaySummary Replay(ReplayConfig config, TextWriter diagnostics)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(diagnostics);
        string path = Path.GetFullPath(config.Path);
        if (!File.Exists(path))
        {
            throw new SimulationUsageException($"record does not exist: {path}");
        }

        using var reader = File.OpenText(path);
        string? first = reader.ReadLine();
        if (first is null)
        {
            throw new SimulationUsageException("record is empty");
        }

        using (var document = JsonDocument.Parse(first))
        {
            RequireEqual("header", RecordType(document.RootElement), "first record type");
        }

        var header = Read<HeaderRecord>(first, "header");
        if (header.Schema != RecordSchema)
        {
            throw new SimulationUsageException(
                $"record schema {header.Schema} is not supported; expected {RecordSchema}");
        }

        if (!string.Equals(header.Policy, ActingPolicy.Name, StringComparison.Ordinal)
            || header.PolicyVersion != ActingPolicy.Version)
        {
            throw new SimulationUsageException(
                $"policy {header.Policy} v{header.PolicyVersion} is not available");
        }

        RequireEqual(
            ActingPolicy.Visibility,
            header.PolicyVisibility,
            "policy visibility");
        ValidateHeaderSeeds(header);

        var data = SimulationData.Load(config.RepoRoot);
        var simulation = new SimulationConfig(
            header.Scenario, header.Difficulty, header.Heroes, header.ModularSets, 1, [], null,
            header.SelectionSeed, 0, header.DecisionLimit, null, config.RepoRoot);
        Validate(data.Setup, simulation);

        Game? game = null;
        ActingPolicy? replayPolicy = null;
        int currentGame = -1;
        int currentSteps = 0;
        int games = 0;
        int steps = 0;
        int replayPlayerWins = 0;
        int replayVillainWins = 0;
        int replayPlayerLosses = 0;
        int replayFailures = 0;
        int replayRounds = 0;
        int replayCardsPlayed = 0;
        int replayPlayerAttacks = 0;
        int replayPayments = 0;
        int replayResourceAbilities = 0;
        var replaySignatures = new Dictionary<string, int>(StringComparer.Ordinal);
        var replayRecent = new Queue<StepRecord>();
        bool sawSummary = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (sawSummary)
            {
                throw new ReplayDivergenceException(
                    "a record appeared after the terminal summary");
            }

            using var document = JsonDocument.Parse(line);
            string type = RecordType(document.RootElement);
            switch (type)
            {
                case "start":
                {
                    var start = Read<StartRecord>(line, type);
                    if (game is not null)
                    {
                        throw new ReplayDivergenceException(
                            $"game {start.Game} started before game {currentGame} ended");
                    }

                    RequireEqual(games, start.Game, $"start game index");
                    var opened = Open(data, simulation, start.Seed);
                    game = opened.Game;
                    replayPolicy = new ActingPolicy(data.Cards, start.SeatPolicySeeds);
                    currentGame = start.Game;
                    currentSteps = 0;
                    replayRecent.Clear();
                    if (start.Game < 0 || start.Game >= header.Seeds.Count)
                    {
                        throw new ReplayDivergenceException(
                            $"start names game index {start.Game} outside the seed plan");
                    }

                    RequireEqual(
                        header.Seeds[start.Game], start.Seed,
                        $"game {start.Game} planned seed");
                    RequireEqual(
                        unchecked(header.PolicySeed + (uint)start.Game),
                        start.PolicySeed,
                        $"game {start.Game} policy master seed");
                    games++;
                    RequireEqual(
                        start.InitialDigest,
                        game.State.Digest().Canonical(),
                        $"game {currentGame} initial digest");
                    RequireSequence(
                        start.SeatPolicySeeds,
                        SeatPolicySeeds(start.PolicySeed, header.Heroes.Count),
                        $"game {currentGame} seat policy seeds");
                    RequireEvents(
                        start.SetupEvents,
                        opened.SetupEvents,
                        $"game {currentGame} setup events");
                    break;
                }
                case "step":
                {
                    var step = Read<StepRecord>(line, type);
                    RequireGame(game, currentGame, step.Game);
                    RequireEqual(
                        currentSteps, step.Step,
                        $"game {currentGame} step index");
                    var asked = game!.Pending
                        ?? throw new ReplayDivergenceException(
                            $"game {currentGame} ended before recorded step {step.Step}");
                    RequirePrompt(
                        step.Prompt,
                        PromptRecord.From(asked),
                        $"game {currentGame} step {step.Step} prompt");
                    var policyDecision = replayPolicy!.Answer(game);
                    RequireEqual(
                        JsonSerializer.Serialize(
                            step.Decision, RecordJson.Options),
                        JsonSerializer.Serialize(
                            DecisionSelector.From(asked, policyDecision), RecordJson.Options),
                        $"game {currentGame} step {step.Step} policy decision");
                    RequireSequence(
                        step.Targets,
                        policyDecision.Targets,
                        $"game {currentGame} step {step.Step} policy targets");
                    RequireSequence(
                        step.Resources,
                        policyDecision.Spent,
                        $"game {currentGame} step {step.Step} policy resources");
                    RequireEqual(
                        JsonSerializer.Serialize(step.Values, RecordJson.Options),
                        JsonSerializer.Serialize(
                            policyDecision.DefinedValues, RecordJson.Options),
                        $"game {currentGame} step {step.Step} policy variables");
                    var decision = step.Decision.Resolve(
                        asked, step.Targets, step.Resources, step.Values);
                    var resolved = game.Resolve(decision);
                    replayPolicy.DecisionResolved();
                    replayRecent.Enqueue(step);
                    while (replayRecent.Count > RecentEventLimit)
                    {
                        replayRecent.Dequeue();
                    }
                    RequireEvents(
                        step.Events,
                        resolved.Events,
                        $"game {currentGame} step {step.Step} events");
                    RequireEqual(
                        step.Digest,
                        game.State.Digest().Fingerprint(),
                        $"game {currentGame} step {step.Step} digest");
                    steps++;
                    currentSteps++;
                    break;
                }
                case "result":
                {
                    var result = Read<ResultRecord>(line, type);
                    RequireGame(game, currentGame, result.Game);
                    RequireEqual(
                        currentSteps, result.Decisions,
                        $"game {currentGame} decision count");
                    RequireEqual(
                        header.Seeds[result.Game], result.Seed,
                        $"game {currentGame} result seed");
                    RequireEqual(
                        result.Outcome,
                        game!.State.Result.ToString(),
                        $"game {currentGame} outcome");
                    RequireEqual(result.Round, game.Round, $"game {currentGame} round");
                    RequireEqual(
                        result.TerminalDigest,
                        game.State.Digest().Canonical(),
                        $"game {currentGame} terminal digest");
                    RequireEqual(
                        JsonSerializer.Serialize(result.Metrics, RecordJson.Options),
                        JsonSerializer.Serialize(replayPolicy!.Metrics, RecordJson.Options),
                        $"game {currentGame} policy metrics");
                    if (game.Pending is not null)
                    {
                        throw new ReplayDivergenceException(
                            $"game {currentGame} has a prompt after its result");
                    }

                    switch (game.State.Result)
                    {
                        case Outcome.PlayersWin:
                            replayPlayerWins++;
                            break;
                        case Outcome.VillainWins:
                            replayVillainWins++;
                            break;
                        case Outcome.PlayersLose:
                            replayPlayerLosses++;
                            break;
                    }

                    replayRounds += game.Round;
                    AddMetrics(
                        replayPolicy.Metrics,
                        ref replayCardsPlayed,
                        ref replayPlayerAttacks,
                        ref replayPayments,
                        ref replayResourceAbilities);
                    game = null;
                    replayPolicy = null;
                    break;
                }
                case "failure":
                {
                    var failure = Read<FailureRecord>(line, type);
                    if (failure.Game < 0 || failure.Game >= header.Seeds.Count)
                    {
                        throw new ReplayDivergenceException(
                            $"failure names game index {failure.Game} outside the seed plan");
                    }

                    RequireEqual(
                        header.Seeds[failure.Game], failure.Seed,
                        $"game {failure.Game} failure seed");
                    if (game is null)
                    {
                        RequireEqual(games, failure.Game, "setup failure game index");
                        if (failure.LastGoodDigest is not null || failure.Decision is not null)
                        {
                            throw new ReplayDivergenceException(
                                $"game {failure.Game} has no start but records gameplay state");
                        }

                        RequireEqual(0, failure.Round, $"game {failure.Game} setup round");
                        RequireEqual(
                            JsonSerializer.Serialize(
                                new PolicyMetrics(0, 0, 0, 0), RecordJson.Options),
                            JsonSerializer.Serialize(failure.Metrics, RecordJson.Options),
                            $"game {failure.Game} setup metrics");
                        RequireNullablePrompt(
                            null, failure.Prompt, $"game {failure.Game} setup prompt");
                        RequireEqual(0, failure.RecentSteps.Count,
                            $"game {failure.Game} setup recent-step count");

                        try
                        {
                            _ = Open(data, simulation, failure.Seed);
                        }
                        catch (Exception error)
                        {
                            RequireFailure(failure, error, "setup");
                            AddFailure(failure, replaySignatures, ref replayFailures);
                            diagnostics.WriteLine(
                                $"reproduced expected setup failure in game {failure.Game}: "
                                + $"{error.GetType().Name}: {error.Message}");
                            games++;
                            currentGame = failure.Game;
                            break;
                        }

                        throw new ReplayDivergenceException(
                            $"game {failure.Game} did not reproduce recorded setup failure");
                    }

                    RequireGame(game, currentGame, failure.Game);
                    RequireEqual(
                        currentSteps, failure.Step,
                        $"game {currentGame} failure step");
                    RequireEqual(
                        failure.LastGoodDigest,
                        game.State.Digest().Canonical(),
                        $"game {currentGame} last-good digest");
                    RequireNullablePrompt(
                        failure.Prompt,
                        ExpectedFailurePrompt(failure, game),
                        $"game {currentGame} failure prompt");
                    RequireEqual(
                        JsonSerializer.Serialize(failure.RecentSteps, RecordJson.Options),
                        JsonSerializer.Serialize(replayRecent, RecordJson.Options),
                        $"game {currentGame} recent steps");
                    RequireEqual(
                        game.Round, failure.Round, $"game {currentGame} failure round");
                    RequireEqual(
                        JsonSerializer.Serialize(
                            replayPolicy!.Metrics, RecordJson.Options),
                        JsonSerializer.Serialize(failure.Metrics, RecordJson.Options),
                        $"game {currentGame} failure metrics");
                    AddFailure(failure, replaySignatures, ref replayFailures);
                    replayRounds += game.Round;
                    AddMetrics(
                        replayPolicy!.Metrics,
                        ref replayCardsPlayed,
                        ref replayPlayerAttacks,
                        ref replayPayments,
                        ref replayResourceAbilities);
                    if (string.Equals(
                            failure.Category, "decision_limit", StringComparison.Ordinal))
                    {
                        string expected = $"decision limit {header.DecisionLimit} reached at "
                            + $"'{game.Pending?.Label}'";
                        RequireEqual(
                            "decision_limit",
                            failure.Category,
                            $"game {currentGame} failure category");
                        RequireEqual(
                            typeof(SimulationRunException).FullName,
                            failure.Exception,
                            $"game {currentGame} failure type");
                        RequireEqual(
                            expected, failure.Message,
                            $"game {currentGame} failure message");
                        RequireEqual(
                            failure.PostFailureDigest,
                            game.State.Digest().Canonical(),
                            $"game {currentGame} post-failure digest");
                        game = null;
                        replayPolicy = null;
                        break;
                    }

                    if (string.Equals(
                            failure.Category, "policy_error", StringComparison.Ordinal))
                    {
                        try
                        {
                            var generated = replayPolicy!.Answer(game);
                            _ = DecisionSelector.From(game.Pending!, generated);
                        }
                        catch (Exception error)
                        {
                            RequireFailure(failure, error, "policy");
                            game = null;
                            replayPolicy = null;
                            break;
                        }

                        throw new ReplayDivergenceException(
                            $"game {currentGame} did not reproduce recorded policy failure");
                    }

                    if (failure.Decision is null)
                    {
                        if (string.Equals(
                                failure.Category, "engine_exception", StringComparison.Ordinal)
                            && game.Pending is null)
                        {
                            var terminal = new SimulationRunException(
                                $"game ended without a terminal outcome: {game.State.Result}");
                            RequireFailure(failure, terminal, "terminal");
                            game = null;
                            replayPolicy = null;
                            break;
                        }

                        throw new ReplayDivergenceException(
                            $"game {currentGame} records category '{failure.Category}' "
                            + "without an attempted decision");
                    }

                    if (game.Pending is null)
                    {
                        throw new ReplayDivergenceException(
                            $"game {currentGame} failure has no pending decision");
                    }

                    try
                    {
                        var decision = failure.Decision.Resolve(
                            game.Pending, failure.Targets, failure.Resources,
                            failure.Values);
                        game.Resolve(decision);
                    }
                    catch (Exception error)
                    {
                        RequireFailure(failure, error, "resolve");
                        RequireEqual(
                            failure.PostFailureDigest,
                            game.State.Digest().Canonical(),
                            $"game {currentGame} post-failure digest");
                        diagnostics.WriteLine(
                            $"reproduced expected failure in game {currentGame}: "
                            + $"{error.GetType().Name}: {error.Message}");
                        game = null;
                        replayPolicy = null;
                        break;
                    }

                    throw new ReplayDivergenceException(
                        $"game {currentGame} did not reproduce recorded failure");
                }
                case "summary":
                {
                    if (game is not null)
                    {
                        throw new ReplayDivergenceException(
                            $"summary appeared while game {currentGame} was active");
                    }

                    var summary = Read<SummaryRecord>(line, type);
                    RequireEqual(games, summary.Games, "summary game count");
                    RequireEqual(replayPlayerWins, summary.PlayersWin, "summary player wins");
                    RequireEqual(replayVillainWins, summary.VillainWins, "summary villain wins");
                    RequireEqual(replayPlayerLosses, summary.PlayersLose, "summary player losses");
                    RequireEqual(replayFailures, summary.Failures, "summary failures");
                    RequireEqual(steps, summary.Decisions, "summary decisions");
                    RequireEqual(replayRounds, summary.Rounds, "summary rounds");
                    RequireEqual(replayCardsPlayed, summary.CardsPlayed, "summary cards played");
                    RequireEqual(replayPlayerAttacks, summary.PlayerAttacks, "summary player attacks");
                    RequireEqual(replayPayments, summary.Payments, "summary payments");
                    RequireEqual(
                        replayResourceAbilities,
                        summary.ResourceAbilitiesUsed,
                        "summary resource abilities");
                    RequireEqual(
                        JsonSerializer.Serialize(replaySignatures, RecordJson.Options),
                        JsonSerializer.Serialize(summary.FailureSignatures, RecordJson.Options),
                        "summary failure signatures");
                    sawSummary = true;
                    break;
                }
                default:
                    throw new SimulationUsageException($"unknown record type '{type}'");
            }
        }

        if (game is not null)
        {
            throw new ReplayDivergenceException(
                $"record ended while game {currentGame} was still open");
        }

        RequireEqual(header.Seeds.Count, games, "recorded game count");
        if (!sawSummary)
        {
            throw new ReplayDivergenceException("record has no terminal summary");
        }

        return new ReplaySummary(games, steps);
    }

    public static SimulationSummary Report(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new SimulationUsageException($"record does not exist: {fullPath}");
        }

        SummaryRecord? found = null;
        HeaderRecord? reportHeader = null;
        bool sawAny = false;
        int games = 0;
        int playerWins = 0;
        int villainWins = 0;
        int playerLosses = 0;
        int failures = 0;
        int decisions = 0;
        int rounds = 0;
        int cardsPlayed = 0;
        int playerAttacks = 0;
        int payments = 0;
        int resourceAbilities = 0;
        var signatures = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string line in File.ReadLines(fullPath))
        {
            if (found is not null)
            {
                throw new ReplayDivergenceException(
                    "a record appeared after the terminal summary");
            }

            using var document = JsonDocument.Parse(line);
            string type = RecordType(document.RootElement);
            switch (type)
            {
                case "header":
                    if (sawAny)
                    {
                        throw new ReplayDivergenceException(
                            "header is not the first record");
                    }

                    reportHeader = Read<HeaderRecord>(line, type);
                    break;
                case "start":
                    break;
                case "step":
                    decisions++;
                    break;
                case "result":
                {
                    var result = Read<ResultRecord>(line, type);
                    RequireEqual(games, result.Game, "result game index");
                    games++;
                    rounds += result.Round;
                    AddMetrics(
                        result.Metrics,
                        ref cardsPlayed,
                        ref playerAttacks,
                        ref payments,
                        ref resourceAbilities);
                    switch (result.Outcome)
                    {
                        case nameof(Outcome.PlayersWin):
                            playerWins++;
                            break;
                        case nameof(Outcome.VillainWins):
                            villainWins++;
                            break;
                        case nameof(Outcome.PlayersLose):
                            playerLosses++;
                            break;
                        default:
                            throw new ReplayDivergenceException(
                                $"unknown result outcome '{result.Outcome}'");
                    }

                    break;
                }
                case "failure":
                {
                    var failure = Read<FailureRecord>(line, type);
                    RequireEqual(games, failure.Game, "failure game index");
                    games++;
                    rounds += failure.Round;
                    AddMetrics(
                        failure.Metrics,
                        ref cardsPlayed,
                        ref playerAttacks,
                        ref payments,
                        ref resourceAbilities);
                    AddFailure(failure, signatures, ref failures);
                    break;
                }
                case "summary":
                    if (found is not null)
                    {
                        throw new ReplayDivergenceException(
                            "record contains more than one summary");
                    }

                    found = Read<SummaryRecord>(line, type);
                    break;
                default:
                    throw new SimulationUsageException($"unknown record type '{type}'");
            }

            sawAny = true;
        }

        if (found is null)
        {
            throw new SimulationUsageException("record has no summary");
        }

        if (reportHeader is null)
        {
            throw new ReplayDivergenceException("record has no header");
        }

        if (reportHeader.Schema != RecordSchema)
        {
            throw new SimulationUsageException(
                $"record schema {reportHeader.Schema} is not supported; "
                + $"expected {RecordSchema}");
        }

        RequireEqual(reportHeader.Seeds.Count, games, "recorded game count");

        var rebuilt = new SummaryRecord(
            "summary", games, playerWins, villainWins, playerLosses, failures,
            decisions, rounds, cardsPlayed, playerAttacks, payments,
            resourceAbilities, signatures);
        RequireEqual(
            JsonSerializer.Serialize(rebuilt, RecordJson.Options),
            JsonSerializer.Serialize(found, RecordJson.Options),
            "aggregate summary");
        return new SimulationSummary(
            rebuilt.Games, rebuilt.PlayersWin, rebuilt.VillainWins,
            rebuilt.PlayersLose, rebuilt.Failures, rebuilt.Decisions,
            rebuilt.Rounds, rebuilt.CardsPlayed, rebuilt.PlayerAttacks,
            rebuilt.Payments, rebuilt.ResourceAbilitiesUsed,
            rebuilt.FailureSignatures);
    }

    private static OpenedGame Open(
        SimulationData data, SimulationConfig config, uint seed)
    {
        var abilities = new AbilityRunner(data.Abilities);
        var setupEvents = new List<GameEvent>();
        var world = WorldSetup.Deal(
            data.Cards,
            Blueprints.From(
                Dealer.DealOrder(
                    data.Setup, CampaignKey(config), config.Heroes, config.ModularSets),
                data.Cards),
            [.. config.Heroes.Select(hero => data.Setup.Hero(hero).Name)],
            seed,
            abilities,
            setupEvents,
            expert: data.Setup.Campaign(CampaignKey(config)).Expert);
        return new OpenedGame(Game.Begin(world, data.Cards, abilities), setupEvents);
    }

    internal static IReadOnlyList<uint> PlanSeeds(SimulationConfig config)
    {
        if (config.ExplicitSeeds.Count > 0)
        {
            return config.ExplicitSeeds;
        }

        if (config.SelectionSeed is { } selectionSeed)
        {
            var selection = new MersenneTwister(selectionSeed);
            var sampled = new uint[config.Games];
            for (int index = 0; index < sampled.Length; index++)
            {
                sampled[index] = selection.NextUInt32();
            }

            return sampled;
        }

        uint start = config.SeedStart ?? 1;
        if ((ulong)start + (ulong)config.Games - 1 > uint.MaxValue)
        {
            throw new SimulationUsageException(
                $"{config.Games} consecutive seeds overflow after {start}");
        }

        var consecutive = new uint[config.Games];
        for (int index = 0; index < consecutive.Length; index++)
        {
            consecutive[index] = checked(start + (uint)index);
        }

        return consecutive;
    }

    internal static uint[] SeatPolicySeeds(uint seed, int players)
    {
        var planner = new MersenneTwister(seed);
        var seeds = new uint[players];
        for (int player = 0; player < players; player++)
        {
            seeds[player] = planner.NextUInt32();
        }

        return seeds;
    }

    private static void Validate(SetupCatalog setup, SimulationConfig config)
    {
        try
        {
            string campaignKey = CampaignKey(config);
            var campaign = setup.Campaign(campaignKey);
            bool expert = string.Equals(config.Difficulty, "expert", StringComparison.Ordinal);
            if (campaign.Expert != expert)
            {
                throw new SimulationUsageException(
                    $"campaign '{campaignKey}' does not match {config.Difficulty} difficulty");
            }
            foreach (string hero in config.Heroes)
            {
                _ = setup.Hero(hero);
            }

            if (config.Heroes.Distinct(StringComparer.Ordinal).Count() != config.Heroes.Count)
            {
                throw new SimulationUsageException("the same hero cannot occupy two seats");
            }

            if (config.ModularSets is not null)
            {
                if (config.ModularSets.Distinct(StringComparer.Ordinal).Count()
                    != config.ModularSets.Count)
                {
                    throw new SimulationUsageException(
                        "the same modular set cannot be selected twice");
                }

                foreach (string modular in config.ModularSets)
                {
                    _ = setup.EncounterSet(modular);
                }
            }
        }
        catch (KeyNotFoundException error)
        {
            throw new SimulationUsageException(error.Message);
        }
    }

    private static string Reproduce(SimulationConfig config, uint seed, uint policySeed)
    {
        var pieces = new List<string>
        {
            "dotnet run --project src/Marvel.Sim -- run",
            "--scenario", Quote(config.Scenario),
            "--difficulty", config.Difficulty,
        };
        foreach (string hero in config.Heroes)
        {
            pieces.Add("--hero");
            pieces.Add(Quote(hero));
        }

        if (config.ModularSets is null)
        {
            // Omission means the scenario's recommended modular sets.
        }
        else if (config.ModularSets.Count == 0)
        {
            pieces.Add("--no-modulars");
        }
        else
        {
            foreach (string modular in config.ModularSets)
            {
                pieces.Add("--modular");
                pieces.Add(Quote(modular));
            }
        }

        pieces.AddRange([
            "--seed", seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--seed-mode", "explicit",
            "--policy-seed", policySeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--decision-limit",
            config.DecisionLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ]);
        return string.Join(' ', pieces);
    }

    private static string Quote(string value) =>
        value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            ? value
            : "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

    private static string CampaignKey(SimulationConfig config) =>
        string.Equals(config.Difficulty, "expert", StringComparison.Ordinal)
            ? config.Scenario + "_expert"
            : config.Scenario;

    private static T Read<T>(string line, string expectedType) =>
        JsonSerializer.Deserialize<T>(line, RecordJson.Options)
        ?? throw new JsonException($"{expectedType} record was null");

    private static void ValidateHeaderSeeds(HeaderRecord header)
    {
        if (header.Seeds.Count == 0)
        {
            throw new ReplayDivergenceException("header contains no game seeds");
        }

        switch (header.SeedMode)
        {
            case "explicit":
                if (header.SelectionSeed is not null)
                {
                    throw new ReplayDivergenceException(
                        "explicit seed mode records a selection seed");
                }

                break;
            case "consecutive":
                if (header.SelectionSeed is not null
                    || (ulong)header.Seeds[0] + (ulong)header.Seeds.Count - 1
                        > uint.MaxValue
                    || header.Seeds.Where((seed, index) => index > 0
                        && seed != header.Seeds[0] + (uint)index).Any())
                {
                    throw new ReplayDivergenceException(
                        "consecutive seed plan does not match its mode");
                }

                break;
            case "random":
                if (header.SelectionSeed is not { } selectionSeed)
                {
                    throw new ReplayDivergenceException(
                        "random seed mode has no selection seed");
                }

                var planner = new MersenneTwister(selectionSeed);
                var expected = Enumerable.Range(0, header.Seeds.Count)
                    .Select(_ => planner.NextUInt32())
                    .ToList();
                RequireSequence(expected, header.Seeds, "random seed plan");
                break;
            default:
                throw new ReplayDivergenceException(
                    $"unknown seed mode '{header.SeedMode}'");
        }
    }

    private static string RecordType(JsonElement record)
    {
        if (record.ValueKind != JsonValueKind.Object
            || !record.TryGetProperty("type", out var type)
            || type.ValueKind != JsonValueKind.String
            || type.GetString() is not { Length: > 0 } value)
        {
            throw new JsonException("record has no string 'type'");
        }

        return value;
    }

    private static void RequireGame(Game? game, int current, int recorded)
    {
        if (game is null || current != recorded)
        {
            throw new ReplayDivergenceException(
                $"record for game {recorded} appeared while game {current} was active");
        }
    }

    private static void RequireEqual<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new ReplayDivergenceException(
                $"{what} diverged: expected '{expected}', got '{actual}'");
        }
    }

    private static void RequireEvents(
        IReadOnlyList<JsonElement> expected,
        IReadOnlyList<GameEvent> actual,
        string what)
    {
        var written = actual.Select(RecordJson.Event).ToList();
        if (expected.Count != written.Count)
        {
            throw new ReplayDivergenceException(
                $"{what} diverged: expected {expected.Count}, got {written.Count}");
        }

        for (int index = 0; index < expected.Count; index++)
        {
            RequireEqual(
                expected[index].GetRawText(), written[index].GetRawText(),
                $"{what}[{index}]");
        }
    }

    private static void RequireFailure(
        FailureRecord expected, Exception actual, string stage)
    {
        RequireEqual(
            FailureCategory(actual, stage),
            expected.Category,
            $"game {expected.Game} failure category");
        RequireEqual(
            expected.Exception,
            actual.GetType().FullName ?? actual.GetType().Name,
            $"game {expected.Game} failure type");
        RequireEqual(
            expected.Message,
            actual.Message,
            $"game {expected.Game} failure message");
    }

    private static string FailureCategory(Exception error, string stage)
    {
        if (error is SimulationRunException
            && error.Message.StartsWith("decision limit ", StringComparison.Ordinal))
        {
            return "decision_limit";
        }

        if (error is RulesNotImplementedException)
        {
            return "rules_not_implemented";
        }

        if (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return "record_error";
        }

        return string.Equals(stage, "policy", StringComparison.Ordinal)
            ? "policy_error"
            : "engine_exception";
    }

    private static PromptRecord? FailurePrompt(
        string stage,
        DecisionSelector? attempted,
        PromptRecord? beforeDecision,
        Game? game)
    {
        if (string.Equals(stage, "terminal", StringComparison.Ordinal)
            || string.Equals(stage, "setup", StringComparison.Ordinal))
        {
            return null;
        }

        if (attempted is not null)
        {
            return beforeDecision;
        }

        return game?.Pending is null ? beforeDecision : PromptRecord.From(game.Pending);
    }

    private static PromptRecord? ExpectedFailurePrompt(FailureRecord failure, Game game)
    {
        if (string.Equals(failure.Category, "engine_exception", StringComparison.Ordinal)
            && failure.Decision is null
            && game.Pending is null)
        {
            return null;
        }

        return game.Pending is null ? null : PromptRecord.From(game.Pending);
    }

    private static void AddFailure(
        FailureRecord failure,
        Dictionary<string, int> signatures,
        ref int failures)
    {
        failures++;
        string signature = $"{TypeName(failure.Exception)}: {failure.Message}";
        signatures[signature] = signatures.GetValueOrDefault(signature) + 1;
    }

    private static string TypeName(string qualified) =>
        qualified[(qualified.LastIndexOf('.') + 1)..];

    private static void AddMetrics(
        PolicyMetrics metrics,
        ref int cardsPlayed,
        ref int playerAttacks,
        ref int payments,
        ref int resourceAbilities)
    {
        cardsPlayed += metrics.CardsPlayed;
        playerAttacks += metrics.PlayerAttacks;
        payments += metrics.Payments;
        resourceAbilities += metrics.ResourceAbilitiesUsed;
    }

    private static void RequirePrompt(
        PromptRecord expected, PromptRecord actual, string what)
    {
        RequireEqual(
            JsonSerializer.Serialize(expected, RecordJson.Options),
            JsonSerializer.Serialize(actual, RecordJson.Options),
            what);
    }

    private static void RequireNullablePrompt(
        PromptRecord? expected, PromptRecord? actual, string what)
    {
        RequireEqual(
            JsonSerializer.Serialize(expected, RecordJson.Options),
            JsonSerializer.Serialize(actual, RecordJson.Options),
            what);
    }

    private static void RequireSequence<T>(
        IReadOnlyList<T> expected, IReadOnlyList<T> actual, string what)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new ReplayDivergenceException(
                $"{what} diverged: expected [{string.Join(',', expected)}], "
                + $"got [{string.Join(',', actual)}]");
        }
    }

    private sealed record OpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);

    private sealed record SimulationData(
        SetupCatalog Setup, CardCatalog Cards, AbilityBook Abilities)
    {
        public static SimulationData Load(string? requestedRoot)
        {
            string root = RepositoryRoot(requestedRoot);
            return new SimulationData(
                SetupCatalog.Parse(File.ReadAllText(
                    Path.Combine(root, "datasets", "setup", "setup.json"))),
                CardCatalog.Parse(File.ReadAllText(
                    Path.Combine(root, "datasets", "cards", "cards.json"))),
                AbilityCatalog.Parse(File.ReadAllText(
                    Path.Combine(root, "datasets", "abilities", "abilities.json"))));
        }

        private static string RepositoryRoot(string? requested)
        {
            if (requested is not null)
            {
                string root = Path.GetFullPath(requested);
                if (!File.Exists(Path.Combine(root, "Marvel.slnx")))
                {
                    throw new SimulationUsageException(
                        $"--repo-root does not contain Marvel.slnx: {root}");
                }

                return root;
            }

            for (DirectoryInfo? directory = new(Environment.CurrentDirectory);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Marvel.slnx")))
                {
                    return directory.FullName;
                }
            }

            throw new SimulationUsageException(
                "could not find Marvel.slnx; run from the repository or pass --repo-root");
        }
    }
}

internal sealed class SimulationRunException(string message) : Exception(message);
