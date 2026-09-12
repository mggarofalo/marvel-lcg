using System.Globalization;

namespace Marvel.Sim;

/// <summary>Parses and validates one simulation-run command.</summary>
internal static class SimulationRunArguments
{
    internal static SimulationConfig Parse(string[] args)
    {
        var state = new RunState();
        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];
            if (!state.TryApplySetup(option, args, ref index)
                && !state.TryApplySeed(option, args, ref index)
                && !state.TryApplyOutput(option, args, ref index))
            {
                throw new SimulationUsageException($"unknown run option '{option}'");
            }
        }
        return state.ToConfig();
    }

    private sealed class RunState
    {
        private string? scenario;
        private string? difficulty;
        private readonly List<string> heroes = [];
        private List<string>? modulars;
        private bool noModulars;
        private readonly List<uint> explicitSeeds = [];
        private uint? seedStart;
        private uint? selectionSeed;
        private string? requestedSeedMode;
        private uint policySeed = 266;
        private int games = 1;
        private bool gamesSpecified;
        private int decisionLimit = 600;
        private string? output;
        private string? repoRoot;

        internal bool TryApplySetup(string option, string[] args, ref int index)
        {
            switch (option)
            {
                case "--scenario": scenario = Value(args, ref index, option); return true;
                case "--hero": heroes.Add(Value(args, ref index, option)); return true;
                case "--difficulty": difficulty = Value(args, ref index, option); return true;
                case "--modular": AddModular(Value(args, ref index, option)); return true;
                case "--no-modulars": RemoveModulars(); return true;
                case "--games":
                    games = PositiveInt(Value(args, ref index, option), option);
                    gamesSpecified = true;
                    return true;
                default: return false;
            }
        }

        internal bool TryApplySeed(string option, string[] args, ref int index)
        {
            switch (option)
            {
                case "--seed": explicitSeeds.Add(UInt(Value(args, ref index, option), option)); return true;
                case "--seed-start": seedStart = UInt(Value(args, ref index, option), option); return true;
                case "--selection-seed": selectionSeed = UInt(Value(args, ref index, option), option); return true;
                case "--seed-mode": requestedSeedMode = Value(args, ref index, option); return true;
                case "--policy": ValidatePolicy(Value(args, ref index, option)); return true;
                case "--policy-seed": policySeed = UInt(Value(args, ref index, option), option); return true;
                default: return false;
            }
        }

        internal bool TryApplyOutput(string option, string[] args, ref int index)
        {
            switch (option)
            {
                case "--decision-limit": decisionLimit = PositiveInt(Value(args, ref index, option), option); return true;
                case "--output": output = Value(args, ref index, option); return true;
                case "--repo-root": repoRoot = Value(args, ref index, option); return true;
                default: return false;
            }
        }

        internal SimulationConfig ToConfig()
        {
            ValidateRequiredOptions();
            ValidateSeedOptions();
            ResolveGameCount();
            return new SimulationConfig(
                scenario!, difficulty!, heroes, modulars, games, explicitSeeds, seedStart,
                selectionSeed, policySeed, decisionLimit, output, repoRoot);
        }

        private void AddModular(string modular)
        {
            if (noModulars)
            {
                throw new SimulationUsageException("--modular cannot accompany --no-modulars");
            }
            modulars ??= [];
            modulars.Add(modular);
        }

        private void RemoveModulars()
        {
            if (modulars is { Count: > 0 })
            {
                throw new SimulationUsageException("--no-modulars cannot accompany --modular");
            }
            noModulars = true;
            modulars = [];
        }

        private static void ValidatePolicy(string policy)
        {
            if (policy is not ("acting" or "acting@1"))
            {
                throw new SimulationUsageException(
                    $"unknown policy '{policy}'; only acting@1 is available");
            }
        }

        private void ValidateRequiredOptions()
        {
            if (scenario is null) throw new SimulationUsageException("--scenario is required");
            if (difficulty is not ("standard" or "expert"))
            {
                throw new SimulationUsageException(
                    "--difficulty is required and must be 'standard' or 'expert'");
            }
            if (heroes.Count is < 1 or > 4)
            {
                throw new SimulationUsageException("run requires between one and four --hero values");
            }
        }

        private void ValidateSeedOptions()
        {
            int seedModes = (explicitSeeds.Count > 0 ? 1 : 0)
                + (seedStart.HasValue ? 1 : 0) + (selectionSeed.HasValue ? 1 : 0);
            if (seedModes > 1)
            {
                throw new SimulationUsageException(
                    "choose only one of --seed, --seed-start, or --selection-seed");
            }
            ValidateRequestedSeedMode(InferredSeedMode());
        }

        private string InferredSeedMode() => explicitSeeds.Count > 0 ? "explicit"
            : selectionSeed.HasValue ? "random" : "consecutive";

        private void ValidateRequestedSeedMode(string inferred)
        {
            if (requestedSeedMode is not null && !IsSeedMode(requestedSeedMode))
            {
                throw new SimulationUsageException(
                    "--seed-mode must be 'explicit', 'consecutive', or 'random'");
            }
            if (requestedSeedMode is not null && requestedSeedMode != inferred)
            {
                throw new SimulationUsageException(
                    $"--seed-mode {requestedSeedMode} does not match the supplied seed options");
            }
        }

        private static bool IsSeedMode(string value) =>
            value is "explicit" or "consecutive" or "random";

        private void ResolveGameCount()
        {
            if (explicitSeeds.Count == 0) return;
            if (gamesSpecified && games != explicitSeeds.Count)
            {
                throw new SimulationUsageException(
                    "--games must equal the number of repeated --seed values");
            }
            games = explicitSeeds.Count;
        }
    }
    private static string Value(string[] args, ref int index, string option)
    {
        if (++index >= args.Length)
        {
            throw new SimulationUsageException($"{option} requires a value");
        }
        return args[index];
    }

    private static int PositiveInt(string value, string option) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
        && parsed > 0
            ? parsed
            : throw new SimulationUsageException($"{option} requires a positive integer");

    private static uint UInt(string value, string option) =>
        uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint parsed)
            ? parsed
            : throw new SimulationUsageException($"{option} requires an unsigned integer");
}
