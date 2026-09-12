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
