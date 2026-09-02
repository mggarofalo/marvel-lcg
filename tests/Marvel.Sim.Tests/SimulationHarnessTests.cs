using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Tests;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Session;
using Xunit;

namespace Marvel.Sim.Tests;

public sealed class SimulationHarnessTests
{
    [Fact]
    public void RandomSeedSelectionIsASeparatePinnedMt19937Stream()
    {
        var config = Config(games: 3, seeds: [], selectionSeed: 266);

        Assert.Equal(
            [342154546u, 3107468503u, 10545751u],
            SimulationHarness.PlanSeeds(config));
    }

    [Fact]
    public void ExplicitAndConsecutiveSeedPlansPreserveOrderAndRejectOverflow()
    {
        Assert.Equal(
            [7u, 3u],
            SimulationHarness.PlanSeeds(Config(
                games: 2, seeds: [7, 3], selectionSeed: null)));
        Assert.Equal(
            [40u, 41u, 42u],
            SimulationHarness.PlanSeeds(Config(
                games: 3, seeds: [], selectionSeed: null, seedStart: 40)));
        Assert.Throws<SimulationUsageException>(() =>
            SimulationHarness.PlanSeeds(Config(
                games: 2, seeds: [], selectionSeed: null, seedStart: uint.MaxValue)));
    }

    [Fact]
    public void PolicySeedsAreDerivedPerSeatWithoutUsingTheWorldStream()
    {
        Assert.Equal(
            [396011577u, 2297285372u],
            SimulationHarness.SeatPolicySeeds(9001, players: 2));
    }

    [Fact]
    public void AStableSelectorUsesActorLabelAndOrderedOccurrence()
    {
        var original = Prompt(
            new Affordance(10, "Action", 7, 1, "Choose"),
            new Affordance(11, "Action", 7, 1, "Choose"));
        var selector = DecisionSelector.From(original, Decision.Take(11));
        var replayed = Prompt(
            new Affordance(110, "Action", 7, 1, "Choose"),
            new Affordance(111, "Action", 7, 1, "Choose"));

        Assert.Equal(
            111,
            selector.Resolve(
                1,
                replayed, [], [],
                new Dictionary<string, long>(StringComparer.Ordinal), []).Affordance);
        Assert.Throws<ReplayDivergenceException>(() =>
            selector.Resolve(
                0,
                Prompt(new Affordance(1, "Action", 8, 1, "Choose")),
                [], [], new Dictionary<string, long>(StringComparer.Ordinal), []));
    }

    [Fact]
    public void ASoloGameWritesARecordThatReplaysWithoutDivergence()
    {
        RoundTrip(Config(games: 1, seeds: [265], selectionSeed: null));
    }

    [Rule("rr:obligation.4")]
    [Rule("rr:reveal.4.1")]
    [Fact]
    public void ATwoPlayerRecordRoutesAnObligationToTheNamedHeroAndReplays()
    {
        // Seed 5 deals Peter Parker's obligation to seat 1. Its printed "give
        // to" instruction makes seat 0 the revealing player and destination.
        var record = RoundTrip(Config(
            games: 1,
            seeds: [5],
            selectionSeed: null,
            heroes: ["spider_man", "she_hulk"]) with { PolicySeed = 9006 });

        var moves = record
            .Where(item => item.GetProperty("type").GetString() == "step")
            .SelectMany(item => item.GetProperty("events").EnumerateArray())
            .Where(item => item.GetProperty("kind").GetString() == "CardsMoved")
            .ToList();
        Assert.Contains(moves, moved =>
            moved.GetProperty("to").GetProperty("zone").GetString()
                == "DealtEncounterCardsDeck"
            && moved.GetProperty("to").GetProperty("owner").GetInt32() == 1);
        Assert.Contains(moves, moved =>
            moved.GetProperty("to").GetProperty("zone").GetString()
                == "ObligationsArea"
            && moved.GetProperty("to").GetProperty("owner").GetInt32() == 0);
    }

    [Fact]
    public void ADecisionLimitWritesAReproductionCapsuleAndMachineSummary()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var diagnostics = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

        var summary = SimulationHarness.Run(
            Config(games: 1, seeds: [265], selectionSeed: null, decisionLimit: 1),
            record,
            diagnostics);

        Assert.Equal(1, summary.ExitCode);
        var documents = Lines(record).ToList();
        var header = Assert.Single(
            documents, item => item.GetProperty("type").GetString() == "header");
        Assert.Equal(2, header.GetProperty("policy_version").GetInt32());
        var first = Assert.Single(
            documents, item => item.GetProperty("type").GetString() == "step");
        Assert.False(first.GetProperty("prompt").GetProperty("cancellable").GetBoolean());
        Assert.False(first.GetProperty("decision").GetProperty("decline").GetBoolean());
        var failure = Assert.Single(
            documents, item => item.GetProperty("type").GetString() == "failure");
        Assert.Equal("decision_limit", failure.GetProperty("category").GetString());
        Assert.Contains("--seed 265", failure.GetProperty("reproduce").GetString());
        Assert.NotNull(failure.GetProperty("last_good_digest").GetString());
        Assert.NotNull(failure.GetProperty("post_failure_digest").GetString());
        var machine = Assert.Single(
            documents, item => item.GetProperty("type").GetString() == "summary");
        Assert.Equal(1, machine.GetProperty("failures").GetInt32());

        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-failure-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, record.ToString());
            Assert.Equal(
                1,
                SimulationHarness.Replay(
                    new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FailedGamesKeepMetricsFromDecisionsThatResolvedBeforeTheLimit()
    {
        var summary = SimulationHarness.Run(
            Config(games: 1, seeds: [265], selectionSeed: null, decisionLimit: 20),
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, summary.Failures);
        Assert.True(summary.CardsPlayed > 0);
        Assert.True(summary.Payments > 0);
    }

    [Fact]
    public void ReplayStopsAtTheFirstChangedEventList()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        SimulationHarness.Run(
            Config(games: 1, seeds: [265], selectionSeed: null),
            record,
            TextWriter.Null);
        var lines = record.ToString()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        int changed = lines.FindIndex(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.GetProperty("type").GetString() == "step"
                && document.RootElement.GetProperty("events").GetArrayLength() > 0;
        });
        Assert.True(changed >= 0);
        var step = JsonNode.Parse(lines[changed])!.AsObject();
        step["events"] = new JsonArray();
        lines[changed] = step.ToJsonString(RecordJson.Options);

        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-divergence-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<ReplayDivergenceException>(() =>
                SimulationHarness.Replay(
                    new ReplayConfig(path, RepositoryRoot()), TextWriter.Null));
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
        SimulationHarness.Run(
            Config(games: 1, seeds: [265], selectionSeed: null),
            record,
            TextWriter.Null);
        var lines = record.ToString()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        int changed = lines.FindIndex(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.GetProperty("type").GetString() == "step"
                && document.RootElement.GetProperty("targets").GetArrayLength() > 0;
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
        SimulationHarness.Run(
            Config(games: 1, seeds: [265], selectionSeed: null),
            record,
            TextWriter.Null);
        var lines = record.ToString()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        lines[1] = "{}";

        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-malformed-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<JsonException>(() => SimulationHarness.Replay(
                new ReplayConfig(path, RepositoryRoot()), TextWriter.Null));
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
        int changed = lines.FindIndex(line => JsonNode.Parse(line)!["type"]!.GetValue<string>()
            == "result");
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
        int changed = lines.FindIndex(line => JsonNode.Parse(line)!["type"]!.GetValue<string>()
            == "summary");
        var summary = JsonNode.Parse(lines[changed])!.AsObject();
        summary["players_win"] = 999999;
        lines[changed] = summary.ToJsonString(RecordJson.Options);

        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-report-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<ReplayDivergenceException>(() => SimulationHarness.Report(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void InvalidConfigurationDoesNotCreateTheRequestedOutput()
    {
        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-invalid-{Guid.NewGuid():N}.jsonl");
        Assert.Throws<SimulationUsageException>(() => CommandLine.Run(
            [
                "run", "--scenario", "not_a_scenario", "--difficulty", "standard",
                "--hero", "spider_man", "--seed", "1", "--output", path,
                "--repo-root", RepositoryRoot(),
            ],
            TextWriter.Null,
            TextWriter.Null));
        Assert.False(File.Exists(path));
    }

    private static List<JsonElement> RoundTrip(SimulationConfig config)
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var diagnostics = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var summary = SimulationHarness.Run(config, record, diagnostics);
        Assert.Equal(0, summary.ExitCode);
        Assert.Contains("\"type\":\"result\"", record.ToString());
        Assert.Contains("\"type\":\"summary\"", record.ToString());

        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, record.ToString());
            var replay = SimulationHarness.Replay(
                new ReplayConfig(path, RepositoryRoot()), diagnostics);
            Assert.Equal(1, replay.Games);
            Assert.True(replay.Steps > 0);
            Assert.Equal(1, SimulationHarness.Report(path).Games);
            return Lines(record).ToList();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void AssertReplayDiverges(IReadOnlyList<string> lines)
    {
        string path = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-divergence-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, lines);
            Assert.Throws<ReplayDivergenceException>(() => SimulationHarness.Replay(
                new ReplayConfig(path, RepositoryRoot()), TextWriter.Null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static List<string> SuccessfulLines()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        SimulationHarness.Run(
            Config(games: 1, seeds: [265], selectionSeed: null),
            record,
            TextWriter.Null);
        return record.ToString()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static IEnumerable<JsonElement> Lines(StringWriter writer)
    {
        foreach (string line in writer.ToString().Split(
                     ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            using var document = JsonDocument.Parse(line);
            yield return document.RootElement.Clone();
        }
    }

    private static Prompt Prompt(params Affordance[] options) => new(
        0,
        Question.TurnOption,
        TimingPriority.Untimed,
        "WhenPlayerInTurn",
        "turn",
        true,
        options);

    private static SimulationConfig Config(
        int games,
        IReadOnlyList<uint> seeds,
        uint? selectionSeed,
        IReadOnlyList<string>? heroes = null,
        uint? seedStart = null,
        int decisionLimit = 800) => new(
            "rhino",
            "standard",
            heroes ?? ["spider_man"],
            ["bomb_scare"],
            games,
            seeds,
            seedStart,
            selectionSeed,
            9001,
            decisionLimit,
            null,
            RepositoryRoot());

    private static string RepositoryRoot()
    {
        for (DirectoryInfo? directory = new(Environment.CurrentDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Marvel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("test could not find Marvel.slnx");
    }
}
