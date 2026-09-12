namespace Marvel.Sim;

internal static class SimulationReplayHarness
{
    public static ReplaySummary Replay(ReplayConfig config, TextWriter diagnostics)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new SimulationReplaySession(config, diagnostics).Replay();
    }
}
