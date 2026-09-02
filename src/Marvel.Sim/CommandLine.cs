using System.Globalization;

namespace Marvel.Sim;

internal static class CommandLine
{
    public const string Usage =
        "Usage:\n"
        + "  Marvel.Sim run --scenario NAME --difficulty standard|expert "
        + "--hero NAME [--hero NAME ...] "
        + "[--modular NAME ...|--no-modulars] [--games N] "
        + "[--seed-mode explicit|consecutive|random] "
        + "[--seed N ...|--seed-start N|--selection-seed N] "
        + "[--policy-seed N] [--decision-limit N] [--output FILE] [--repo-root DIR]\n"
        + "  Marvel.Sim replay RECORD.jsonl [--repo-root DIR]\n"
        + "  Marvel.Sim report RECORD.jsonl";

    public static int Run(string[] args, TextWriter output, TextWriter diagnostics)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (args.Length == 0)
        {
            throw new SimulationUsageException("a command is required");
        }

        return args[0] switch
        {
            "run" => RunGames(ParseRun(args[1..]), output, diagnostics),
            "replay" => Replay(ParseReplay(args[1..]), output, diagnostics),
            "report" => Report(args[1..], output),
            "--help" or "-h" or "help" => Help(output),
            _ => throw new SimulationUsageException($"unknown command '{args[0]}'"),
        };
    }

    private static int Help(TextWriter output)
    {
        output.WriteLine(Usage);
        return 0;
    }

    private static int RunGames(
        SimulationConfig config, TextWriter output, TextWriter diagnostics)
    {
        if (config.Output is null)
        {
            var streamed = SimulationHarness.Run(config, output, diagnostics);
            diagnostics.WriteLine(streamed.Human());
            return streamed.ExitCode;
        }

        SimulationHarness.ValidateConfig(config);
        string path = Path.GetFullPath(config.Output);
        string? parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        using var records = new StreamWriter(path, append: false);
        var summary = SimulationHarness.Run(config, records, diagnostics);
        output.WriteLine(summary.Human(path));
        return summary.ExitCode;
    }

    private static int Replay(ReplayConfig config, TextWriter output, TextWriter diagnostics)
    {
        var summary = SimulationHarness.Replay(config, diagnostics);
        output.WriteLine(
            $"replayed {summary.Games} game(s), {summary.Steps} decision(s), no divergence");
        return 0;
    }

    private static int Report(string[] args, TextWriter output)
    {
        if (args.Length != 1)
        {
            throw new SimulationUsageException("report requires exactly one JSONL record path");
        }

        output.WriteLine(SimulationHarness.Report(args[0]).Human(args[0]));
        return 0;
    }

    private static SimulationConfig ParseRun(string[] args)
    {
        string? scenario = null;
        string? difficulty = null;
        var heroes = new List<string>();
        List<string>? modulars = null;
        bool noModulars = false;
        var explicitSeeds = new List<uint>();
        uint? seedStart = null;
        uint? selectionSeed = null;
        string? requestedSeedMode = null;
        uint policySeed = 266;
        int games = 1;
        bool gamesSpecified = false;
        int decisionLimit = 600;
        string? output = null;
        string? repoRoot = null;

        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];
            switch (option)
            {
                case "--scenario":
                    scenario = Value(args, ref index, option);
                    break;
                case "--hero":
                    heroes.Add(Value(args, ref index, option));
                    break;
                case "--difficulty":
                    difficulty = Value(args, ref index, option);
                    break;
                case "--modular":
                    if (noModulars)
                    {
                        throw new SimulationUsageException(
                            "--modular cannot accompany --no-modulars");
                    }

                    modulars ??= [];
                    modulars.Add(Value(args, ref index, option));
                    break;
                case "--no-modulars":
                    if (modulars is { Count: > 0 })
                    {
                        throw new SimulationUsageException(
                            "--no-modulars cannot accompany --modular");
                    }

                    noModulars = true;
                    modulars = [];
                    break;
                case "--games":
                    games = PositiveInt(Value(args, ref index, option), option);
                    gamesSpecified = true;
                    break;
                case "--seed":
                    explicitSeeds.Add(UInt(Value(args, ref index, option), option));
                    break;
                case "--seed-start":
                    seedStart = UInt(Value(args, ref index, option), option);
                    break;
                case "--selection-seed":
                    selectionSeed = UInt(Value(args, ref index, option), option);
                    break;
                case "--seed-mode":
                    requestedSeedMode = Value(args, ref index, option);
                    break;
                case "--policy":
                    string policy = Value(args, ref index, option);
                    if (policy is not ("acting" or "acting@1"))
                    {
                        throw new SimulationUsageException(
                            $"unknown policy '{policy}'; only acting@1 is available");
                    }

                    break;
                case "--policy-seed":
                    policySeed = UInt(Value(args, ref index, option), option);
                    break;
                case "--decision-limit":
                    decisionLimit = PositiveInt(Value(args, ref index, option), option);
                    break;
                case "--output":
                    output = Value(args, ref index, option);
                    break;
                case "--repo-root":
                    repoRoot = Value(args, ref index, option);
                    break;
                default:
                    throw new SimulationUsageException($"unknown run option '{option}'");
            }
        }

        if (scenario is null)
        {
            throw new SimulationUsageException("--scenario is required");
        }

        if (difficulty is not ("standard" or "expert"))
        {
            throw new SimulationUsageException(
                "--difficulty is required and must be 'standard' or 'expert'");
        }

        if (heroes.Count is < 1 or > 4)
        {
            throw new SimulationUsageException("run requires between one and four --hero values");
        }

        int seedModes = (explicitSeeds.Count > 0 ? 1 : 0)
            + (seedStart.HasValue ? 1 : 0)
            + (selectionSeed.HasValue ? 1 : 0);
        if (seedModes > 1)
        {
            throw new SimulationUsageException(
                "choose only one of --seed, --seed-start, or --selection-seed");
        }

        string inferredSeedMode = explicitSeeds.Count > 0 ? "explicit"
            : selectionSeed.HasValue ? "random"
            : "consecutive";
        if (requestedSeedMode is not null
            && requestedSeedMode is not ("explicit" or "consecutive" or "random"))
        {
            throw new SimulationUsageException(
                "--seed-mode must be 'explicit', 'consecutive', or 'random'");
        }

        if (requestedSeedMode is not null
            && !string.Equals(requestedSeedMode, inferredSeedMode, StringComparison.Ordinal))
        {
            throw new SimulationUsageException(
                $"--seed-mode {requestedSeedMode} does not match the supplied seed options");
        }

        if (explicitSeeds.Count > 0)
        {
            if (gamesSpecified && games != explicitSeeds.Count)
            {
                throw new SimulationUsageException(
                    "--games must equal the number of repeated --seed values");
            }

            games = explicitSeeds.Count;
        }

        return new SimulationConfig(
            scenario, difficulty, heroes, modulars, games, explicitSeeds, seedStart,
            selectionSeed, policySeed, decisionLimit, output, repoRoot);
    }

    private static ReplayConfig ParseReplay(string[] args)
    {
        if (args.Length == 0)
        {
            throw new SimulationUsageException("replay requires a JSONL record path");
        }

        string path = args[0];
        string? repoRoot = null;
        for (int index = 1; index < args.Length; index++)
        {
            if (args[index] != "--repo-root")
            {
                throw new SimulationUsageException($"unknown replay option '{args[index]}'");
            }

            repoRoot = Value(args, ref index, "--repo-root");
        }

        return new ReplayConfig(path, repoRoot);
    }

    private static string Value(string[] args, ref int index, string option)
    {
        if (++index >= args.Length)
        {
            throw new SimulationUsageException($"{option} requires a value");
        }

        return args[index];
    }

    private static int PositiveInt(string text, string option) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
        && value > 0
            ? value
            : throw new SimulationUsageException($"{option} requires a positive integer");

    private static uint UInt(string text, string option) =>
        uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out uint value)
            ? value
            : throw new SimulationUsageException($"{option} requires an unsigned 32-bit integer");
}

internal sealed record SimulationConfig(
    string Scenario,
    string Difficulty,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    int Games,
    IReadOnlyList<uint> ExplicitSeeds,
    uint? SeedStart,
    uint? SelectionSeed,
    uint PolicySeed,
    int DecisionLimit,
    string? Output,
    string? RepoRoot);

internal sealed record ReplayConfig(string Path, string? RepoRoot);

internal sealed class SimulationUsageException(string message) : Exception(message);
