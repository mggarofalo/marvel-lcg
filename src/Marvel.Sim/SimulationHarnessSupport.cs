using Marvel.Rules.Setup;
using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;

using static Marvel.Sim.SimulationRunHarness;
using static Marvel.Sim.SimulationReplayHarness;
using static Marvel.Sim.SimulationReportReader;
using static Marvel.Sim.SimulationHarnessSupport;
namespace Marvel.Sim;

internal static class SimulationHarnessSupport
{
    internal const int RecentEventLimit = 20;

    internal static OpenedGame Open(
        SimulationData data, SimulationConfig config, uint seed)
    {
        var abilities = new AbilityRunner(data.Abilities);
        var setupEvents = new List<GameEvent>();
        var world = WorldSetup.Deal(
            data.Cards,
            Blueprints.From(
                Dealer.DealOrder(
                    data.Setup, CampaignKey(config), config.Heroes, config.ModularSets,
                    data.Cards),
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

    internal static void Validate(SetupCatalog setup, SimulationConfig config)
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

    internal static string Reproduce(SimulationConfig config, uint seed, uint policySeed)
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

    internal static string Quote(string value) =>
        value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            ? value
            : "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

    internal static string CampaignKey(SimulationConfig config) =>
        string.Equals(config.Difficulty, "expert", StringComparison.Ordinal)
            ? config.Scenario + "_expert"
            : config.Scenario;

    internal static void RequireGame(Game? game, int current, int recorded)
    {
        if (game is null || current != recorded)
        {
            throw new ReplayDivergenceException(
                $"record for game {recorded} appeared while game {current} was active");
        }
    }

    internal static void RequireEqual<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new ReplayDivergenceException(
                $"{what} diverged: expected '{expected}', got '{actual}'");
        }
    }

    internal static void RequireFailure(
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

    internal static string FailureCategory(Exception error, string stage)
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

    internal static PromptRecord? FailurePrompt(
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

    internal static PromptRecord? ExpectedFailurePrompt(FailureRecord failure, Game game)
    {
        if (string.Equals(failure.Category, "engine_exception", StringComparison.Ordinal)
            && failure.Decision is null
            && game.Pending is null)
        {
            return null;
        }

        return game.Pending is null ? null : PromptRecord.From(game.Pending);
    }

    internal static void AddFailure(
        FailureRecord failure,
        Dictionary<string, int> signatures,
        ref int failures)
    {
        failures++;
        string signature = $"{TypeName(failure.Exception)}: {failure.Message}";
        signatures[signature] = signatures.GetValueOrDefault(signature) + 1;
    }

    internal static string TypeName(string qualified) =>
        qualified[(qualified.LastIndexOf('.') + 1)..];

    internal static void AddMetrics(
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

    internal static void RequireNullablePrompt(
        PromptRecord? expected, PromptRecord? actual, string what) =>
        JournalReplay.RequirePromptRecords(expected, actual, what);

    internal static void RequireSequence<T>(
        IReadOnlyList<T> expected, IReadOnlyList<T> actual, string what)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new ReplayDivergenceException(
                $"{what} diverged: expected [{string.Join(',', expected)}], "
                + $"got [{string.Join(',', actual)}]");
        }
    }

}
