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

    [Fact]
    public void ACompressedSoloRunPreservesJsonlAndReadsByMagicBytes()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-gzip-{Guid.NewGuid():N}");
        string plainPath = Path.Combine(root, "record.jsonl");
        string gzipPath = Path.Combine(root, "record.jsonl.gz");
        string disguisedPath = Path.Combine(root, "record.saved");
        string plainDisguisedPath = Path.Combine(root, "plain.jsonl.gz");
        string concatenatedPath = Path.Combine(root, "concatenated.data");
        try
        {
            Assert.Equal(0, RunTo(plainPath));
            Assert.Equal(0, RunTo(gzipPath));

            byte[] compressed = File.ReadAllBytes(gzipPath);
            Assert.Equal(0x1f, compressed[0]);
            Assert.Equal(0x8b, compressed[1]);
            string plain = File.ReadAllText(plainPath);
            using var gzip = new GZipStream(
                new MemoryStream(compressed), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            Assert.Equal(plain, reader.ReadToEnd());
            Assert.True(compressed.Length < Encoding.UTF8.GetByteCount(plain) / 2);

            File.Move(gzipPath, disguisedPath);
            Assert.Equal(
                1,
                SimulationHarness.Replay(
                    new ReplayConfig(disguisedPath, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationHarness.Report(disguisedPath).Games);

            File.Copy(plainPath, plainDisguisedPath);
            Assert.Equal(1, SimulationHarness.Report(plainDisguisedPath).Games);

            int memberBoundary = plain.IndexOf('\n', StringComparison.Ordinal) + 1;
            File.WriteAllBytes(
                concatenatedPath,
                [
                    .. WithFileName(
                        Gzip(plain[..memberBoundary], CompressionLevel.Optimal),
                        "first.jsonl"),
                    .. WithFileName(
                        Gzip(plain[memberBoundary..], CompressionLevel.Optimal),
                        "second.jsonl"),
                ]);
            Assert.Equal(
                1,
                SimulationHarness.Replay(
                    new ReplayConfig(concatenatedPath, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationHarness.Report(concatenatedPath).Games);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void CorruptAndTruncatedGzipRecordsFailAsUsageErrors()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-sim-bad-gzip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            string corruptPath = Path.Combine(root, "corrupt.jsonl");
            File.WriteAllBytes(corruptPath, [0x1f, 0x8b, 0x08, 0xff, 0xff]);

            var corrupt = Assert.Throws<SimulationUsageException>(() =>
                SimulationHarness.Replay(
                    new ReplayConfig(corruptPath, RepositoryRoot()), TextWriter.Null));
            Assert.Equal(
                $"record is not valid gzip: {Path.GetFullPath(corruptPath)}",
                corrupt.Message);

            string completePath = Path.Combine(root, "complete.jsonl.gz");
            Assert.Equal(0, RunTo(completePath));

            byte[] complete = File.ReadAllBytes(completePath);
            using var source = new GZipStream(
                new MemoryStream(complete), CompressionMode.Decompress);
            using var sourceReader = new StreamReader(source, Encoding.UTF8);
            byte[] changedPayload = Gzip(
                sourceReader.ReadToEnd(), CompressionLevel.NoCompression);
            int changed = Array.IndexOf(changedPayload, (byte)'{', 10);
            Assert.True(changed > 0);
            changedPayload[changed] = (byte)'[';
            string changedPath = Path.Combine(root, "changed-payload.data");
            File.WriteAllBytes(changedPath, changedPayload);
            var changedError = Assert.Throws<SimulationUsageException>(() =>
                SimulationHarness.Report(changedPath));
            Assert.Equal(
                $"record is not valid gzip: {Path.GetFullPath(changedPath)}",
                changedError.Message);

            string truncatedPath = Path.Combine(root, "truncated.data");
            File.WriteAllBytes(truncatedPath, complete[..^8]);
            var truncated = Assert.Throws<SimulationUsageException>(() =>
                SimulationHarness.Report(truncatedPath));
            Assert.Equal(
                $"record is not valid gzip: {Path.GetFullPath(truncatedPath)}",
                truncated.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void StandardOutputRemainsPlainJsonl()
    {
        var output = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(0, CommandLine.Run(
            RunArguments(output: null), output, TextWriter.Null));

        Assert.StartsWith("{\"type\":\"header\"", output.ToString());
        Assert.Contains("\"type\":\"summary\"", output.ToString());
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
            Path.GetTempPath(), $"marvel-sim-invalid-{Guid.NewGuid():N}.jsonl.gz");
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

    private static int RunTo(string path) => CommandLine.Run(
        RunArguments(path), TextWriter.Null, TextWriter.Null);

    private static byte[] Gzip(string content, CompressionLevel level)
    {
        var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, level, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes(content));
        }

        return compressed.ToArray();
    }

    private static byte[] WithFileName(byte[] gzip, string name)
    {
        byte[] header = gzip[..10];
        header[3] |= 0x08;
        return [.. header, .. Encoding.Latin1.GetBytes(name), 0, .. gzip[10..]];
    }

    private static string[] RunArguments(string? output)
    {
        var args = new List<string>
        {
            "run", "--scenario", "rhino", "--difficulty", "standard",
            "--hero", "spider_man", "--modular", "bomb_scare",
            "--seed", "265", "--policy-seed", "9001", "--decision-limit", "800",
            "--repo-root", RepositoryRoot(),
        };
        if (output is not null)
        {
            args.Add("--output");
            args.Add(output);
        }

        return [.. args];
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
