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

internal static class SimulationReportReader
{
    public static SimulationSummary Report(string path)
    {
        return new SimulationReportState().Read(Path.GetFullPath(path));
    }
}
