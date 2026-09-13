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
