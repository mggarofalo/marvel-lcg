namespace Marvel.Sim;

internal static class SimulationHarness
{
    public static void ValidateConfig(SimulationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var data = SimulationData.Load(config.RepoRoot);
        SimulationHarnessSupport.Validate(data.Setup, config);
        _ = PlanSeeds(config);
    }

    public static SimulationSummary Run(
        SimulationConfig config, TextWriter records, TextWriter diagnostics) =>
        SimulationRunHarness.Run(config, records, diagnostics);

    public static ReplaySummary Replay(ReplayConfig config, TextWriter diagnostics) =>
        SimulationReplayHarness.Replay(config, diagnostics);

    public static SimulationSummary Report(string path) =>
        SimulationReportReader.Report(path);

    internal static IReadOnlyList<uint> PlanSeeds(SimulationConfig config) =>
        SimulationHarnessSupport.PlanSeeds(config);

    internal static uint[] SeatPolicySeeds(uint seed, int players) =>
        SimulationHarnessSupport.SeatPolicySeeds(seed, players);

}
