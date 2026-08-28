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
    private const int RecordSchema = 1;
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
            : config.SelectionSeed.HasValue ? "sampled"
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
            DecisionSelector? attempted = null;
            Decision? input = null;
            PromptRecord? prompt = null;
            string? lastGood = null;
            var recent = new Queue<JsonElement>();
            int step = 0;
            try
            {
                var opened = Open(data, config, seed);
                game = opened.Game;
                lastGood = game.State.Digest().Canonical();
                RecordJson.Write(records, new StartRecord(
                    "start", gameIndex, seed, policySeed, policySeeds, lastGood,
                    [.. opened.SetupEvents.Select(RecordJson.Event)]));

                var policy = new ActingPolicy(data.Cards, policySeeds);
                while (game.Pending is not null)
                {
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
                    var resolved = game.Resolve(input);
                    var happened = resolved.Events.Select(RecordJson.Event).ToList();
                    foreach (var item in happened)
                    {
                        recent.Enqueue(item);
                        while (recent.Count > RecentEventLimit)
                        {
                            recent.Dequeue();
                        }
                    }

                    lastGood = game.State.Digest().Canonical();
                    RecordJson.Write(records, new StepRecord(
                        "step", gameIndex, step, prompt, attempted,
                        input.Targets, input.Spent, happened,
                        game.State.Digest().Fingerprint()));
                    step++;
                    decisions++;
                    attempted = null;
                    input = null;
                }

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

                rounds += game.Round;
                cardsPlayed += policy.CardsPlayed;
                playerAttacks += policy.PlayerAttacks;
                payments += policy.Payments;
                resourceAbilities += policy.ResourceAbilitiesUsed;
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
                    "failure", gameIndex, seed, step,
                    error.GetType().FullName ?? error.GetType().Name,
                    error.Message,
                    game?.Pending is null ? prompt : PromptRecord.From(game.Pending),
                    attempted,
                    input?.Targets ?? [],
                    input?.Spent ?? [],
                    lastGood,
                    game?.State.Digest().Canonical(),
                    [.. recent],
                    Reproduce(config, seed, policySeed)));
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

        var data = SimulationData.Load(config.RepoRoot);
        var simulation = new SimulationConfig(
            header.Scenario, header.Difficulty, header.Heroes, header.ModularSets, 1, [], null,
            header.SelectionSeed, 0, header.DecisionLimit, null, config.RepoRoot);
        Validate(data.Setup, simulation);

        Game? game = null;
        int currentGame = -1;
        int games = 0;
        int steps = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            using var document = JsonDocument.Parse(line);
            string type = document.RootElement.GetProperty("type").GetString()
                ?? throw new JsonException("record type was null");
            switch (type)
            {
                case "start":
                {
                    var start = Read<StartRecord>(line, type);
                    var opened = Open(data, simulation, start.Seed);
                    game = opened.Game;
                    currentGame = start.Game;
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
                    var asked = game!.Pending
                        ?? throw new ReplayDivergenceException(
                            $"game {currentGame} ended before recorded step {step.Step}");
                    RequireEqual(
                        step.Prompt,
                        PromptRecord.From(asked),
                        $"game {currentGame} step {step.Step} prompt");
                    var decision = step.Decision.Resolve(asked, step.Targets, step.Resources);
                    var resolved = game.Resolve(decision);
                    RequireEvents(
                        step.Events,
                        resolved.Events,
                        $"game {currentGame} step {step.Step} events");
                    RequireEqual(
                        step.Digest,
                        game.State.Digest().Fingerprint(),
                        $"game {currentGame} step {step.Step} digest");
                    steps++;
                    break;
                }
                case "result":
                {
                    var result = Read<ResultRecord>(line, type);
                    RequireGame(game, currentGame, result.Game);
                    RequireEqual(
                        result.Outcome,
                        game!.State.Result.ToString(),
                        $"game {currentGame} outcome");
                    RequireEqual(result.Round, game.Round, $"game {currentGame} round");
                    RequireEqual(
                        result.TerminalDigest,
                        game.State.Digest().Canonical(),
                        $"game {currentGame} terminal digest");
                    game = null;
                    break;
                }
                case "failure":
                {
                    var failure = Read<FailureRecord>(line, type);
                    RequireGame(game, currentGame, failure.Game);
                    if (failure.Decision is null || game!.Pending is null)
                    {
                        throw new ReplayDivergenceException(
                            $"game {currentGame} failure has no replayable decision");
                    }

                    try
                    {
                        var decision = failure.Decision.Resolve(
                            game.Pending, failure.Targets, failure.Resources);
                        game.Resolve(decision);
                    }
                    catch (Exception error)
                    {
                        RequireEqual(
                            failure.Exception,
                            error.GetType().FullName ?? error.GetType().Name,
                            $"game {currentGame} failure type");
                        RequireEqual(
                            failure.Message,
                            error.Message,
                            $"game {currentGame} failure message");
                        diagnostics.WriteLine(
                            $"reproduced expected failure in game {currentGame}: "
                            + $"{error.GetType().Name}: {error.Message}");
                        game = null;
                        break;
                    }

                    throw new ReplayDivergenceException(
                        $"game {currentGame} did not reproduce recorded failure");
                }
                case "summary":
                    _ = Read<SummaryRecord>(line, type);
                    break;
                default:
                    throw new SimulationUsageException($"unknown record type '{type}'");
            }
        }

        if (game is not null)
        {
            throw new ReplayDivergenceException(
                $"record ended while game {currentGame} was still open");
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
        foreach (string line in File.ReadLines(fullPath))
        {
            using var document = JsonDocument.Parse(line);
            if (document.RootElement.GetProperty("type").GetString() == "summary")
            {
                found = Read<SummaryRecord>(line, "summary");
            }
        }

        if (found is null)
        {
            throw new SimulationUsageException("record has no summary");
        }

        return new SimulationSummary(
            found.Games, found.PlayersWin, found.VillainWins,
            found.PlayersLose, found.Failures, found.Decisions,
            found.Rounds, found.CardsPlayed, found.PlayerAttacks,
            found.Payments, found.ResourceAbilitiesUsed,
            found.FailureSignatures);
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
