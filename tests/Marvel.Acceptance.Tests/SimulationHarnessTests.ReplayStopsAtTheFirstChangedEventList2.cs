using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Tests;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;
public sealed class SimulationHarnessReplayStopsAtTheFirstChangedEventListTests : SimulationHarnessTestBase
{
    [Fact]
    public void ReplayStopsAtTheFirstChangedEventList()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        SimulationRunHarness.Run(Config(games: 1, seeds: [265], selectionSeed: null), record, TextWriter.Null);
        var lines = record.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
        int changed = lines.FindIndex(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.GetProperty("type").GetString() == "step" && document.RootElement.GetProperty("events").GetArrayLength() > 0;
        });
        Assert.True(changed >= 0);
        var step = JsonNode.Parse(lines[changed])!.AsObject();
        step["events"] = new JsonArray();
        lines[changed] = step.ToJsonString(RecordJson.Options);
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-divergence-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<ReplayDivergenceException>(() => SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReplayRejectsATargetOutsideTheRecordedAffordance()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        SimulationRunHarness.Run(Config(games: 1, seeds: [265], selectionSeed: null), record, TextWriter.Null);
        var lines = record.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
        int changed = lines.FindIndex(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.GetProperty("type").GetString() == "step" && document.RootElement.GetProperty("targets").GetArrayLength() > 0;
        });
        var step = JsonNode.Parse(lines[changed])!.AsObject();
        step["targets"] = new JsonArray(999999);
        lines[changed] = step.ToJsonString(RecordJson.Options);
        AssertReplayDiverges(lines);
    }

    [Fact]
    public void ReplayRejectsARecordWithoutAType()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        SimulationRunHarness.Run(Config(games: 1, seeds: [265], selectionSeed: null), record, TextWriter.Null);
        var lines = record.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
        lines[1] = "{}";
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-malformed-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<JsonException>(() => SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReplayRejectsChangedResultMetrics()
    {
        var lines = SuccessfulLines();
        int changed = lines.FindIndex(line => JsonNode.Parse(line)!["type"]!.GetValue<string>() == "result");
        var result = JsonNode.Parse(lines[changed])!.AsObject();
        result["seed"] = 999999;
        result["metrics"]!["cards_played"] = 999999;
        lines[changed] = result.ToJsonString(RecordJson.Options);
        AssertReplayDiverges(lines);
    }

    [Fact]
    public void ReportRejectsAChangedAggregateSummary()
    {
        var lines = SuccessfulLines();
        int changed = lines.FindIndex(line => JsonNode.Parse(line)!["type"]!.GetValue<string>() == "summary");
        var summary = JsonNode.Parse(lines[changed])!.AsObject();
        summary["players_win"] = 999999;
        lines[changed] = summary.ToJsonString(RecordJson.Options);
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-report-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<ReplayDivergenceException>(() => SimulationReportReader.Report(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void InvalidConfigurationDoesNotCreateTheRequestedOutput()
    {
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-invalid-{Guid.NewGuid():N}.jsonl.gz");
        Assert.Throws<SimulationUsageException>(() => CommandLine.Run(["run", "--scenario", "not_a_scenario", "--difficulty", "standard", "--hero", "spider_man", "--seed", "1", "--output", path, "--repo-root", RepositoryRoot(), ], TextWriter.Null, TextWriter.Null));
        Assert.False(File.Exists(path));
    }
}
