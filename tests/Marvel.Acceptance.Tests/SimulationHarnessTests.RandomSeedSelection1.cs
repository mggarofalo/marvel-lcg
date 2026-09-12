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
public sealed class SimulationHarnessRandomSeedSelectionTests : SimulationHarnessTestBase
{
    [Fact]
    public void RandomSeedSelectionIsASeparatePinnedMt19937Stream()
    {
        var config = Config(games: 3, seeds: [], selectionSeed: 266);
        Assert.Equal([342154546u, 3107468503u, 10545751u], SimulationHarness.PlanSeeds(config));
    }

    [Fact]
    public void ExplicitAndConsecutiveSeedPlansPreserveOrderAndRejectOverflow()
    {
        Assert.Equal([7u, 3u], SimulationHarness.PlanSeeds(Config(games: 2, seeds: [7, 3], selectionSeed: null)));
        Assert.Equal([40u, 41u, 42u], SimulationHarness.PlanSeeds(Config(games: 3, seeds: [], selectionSeed: null, seedStart: 40)));
        Assert.Throws<SimulationUsageException>(() => SimulationHarness.PlanSeeds(Config(games: 2, seeds: [], selectionSeed: null, seedStart: uint.MaxValue)));
    }

    [Fact]
    public void PolicySeedsAreDerivedPerSeatWithoutUsingTheWorldStream()
    {
        Assert.Equal([396011577u, 2297285372u], SimulationHarness.SeatPolicySeeds(9001, players: 2));
    }

    [Fact]
    public void AStableSelectorUsesActorLabelAndOrderedOccurrence()
    {
        var original = Prompt(new Affordance(10, "Action", 7, 1, "Choose"), new Affordance(11, "Action", 7, 1, "Choose"));
        var selector = DecisionSelector.From(original, Decision.Take(11));
        var replayed = Prompt(new Affordance(110, "Action", 7, 1, "Choose"), new Affordance(111, "Action", 7, 1, "Choose"));
        Assert.Equal(111, selector.Resolve(1, replayed, [], [], new Dictionary<string, long>(StringComparer.Ordinal), []).Affordance);
        Assert.Throws<ReplayDivergenceException>(() => selector.Resolve(0, Prompt(new Affordance(1, "Action", 8, 1, "Choose")), [], [], new Dictionary<string, long>(StringComparer.Ordinal), []));
    }

    [Fact]
    public void ASoloGameWritesARecordThatReplaysWithoutDivergence()
    {
        RoundTrip(Config(games: 1, seeds: [265], selectionSeed: null));
    }

    [Fact]
    public void CanonicalPromptSchemaShrinksPaymentRecordsAndSchemaTwoStillReplays()
    {
        List<string> current = SuccessfulLines();
        Assert.Equal(3, JsonNode.Parse(current[0])!["schema"]!.GetValue<int>());
        foreach (string line in current)
        {
            JsonNode? node = JsonNode.Parse(line);
            if (node?["type"]?.GetValue<string>() != "step")
            {
                continue;
            }

            foreach (JsonNode? affordance in node["prompt"]!["affordances"]!.AsArray())
            {
                Assert.Null(affordance!["targets"]?["is_grouped"]);
                foreach (JsonNode? cost in affordance["costs"]!.AsArray())
                {
                    Assert.Null(cost!["has_alternative"]);
                    Assert.Null(cost["generators"]);
                    Assert.Null(cost["variable_requests"]);
                    Assert.Null(cost["resource_costs"]);
                }
            }
        }

        List<string> legacy = SchemaTwoLines(current);
        long legacyBytes = legacy.Sum(line => (long)Encoding.UTF8.GetByteCount(line));
        long currentBytes = current.Sum(line => (long)Encoding.UTF8.GetByteCount(line));
        Assert.True(currentBytes * 100 <= legacyBytes * 95, $"schema 3 used {currentBytes} bytes versus schema 2's {legacyBytes}");
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-schema-two-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, legacy);
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationReportReader.Report(path).Games);
            File.WriteAllLines(path, OldestSchemaTwoLines(legacy));
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationReportReader.Report(path).Games);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ACompressedSoloRunPreservesJsonlAndReadsByMagicBytes()
    {
        string root = Path.Combine(Path.GetTempPath(), $"marvel-sim-gzip-{Guid.NewGuid():N}");
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
            using var gzip = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            Assert.Equal(plain, reader.ReadToEnd());
            Assert.True(compressed.Length < Encoding.UTF8.GetByteCount(plain) / 2);
            File.Move(gzipPath, disguisedPath);
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(disguisedPath, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationReportReader.Report(disguisedPath).Games);
            File.Copy(plainPath, plainDisguisedPath);
            Assert.Equal(1, SimulationReportReader.Report(plainDisguisedPath).Games);
            int memberBoundary = plain.IndexOf('\n', StringComparison.Ordinal) + 1;
            File.WriteAllBytes(concatenatedPath, [..WithFileName(Gzip(plain[..memberBoundary], CompressionLevel.Optimal), "first.jsonl"), ..WithFileName(Gzip(plain[memberBoundary..], CompressionLevel.Optimal), "second.jsonl"), ]);
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(concatenatedPath, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationReportReader.Report(concatenatedPath).Games);
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
        string root = Path.Combine(Path.GetTempPath(), $"marvel-sim-bad-gzip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            string corruptPath = Path.Combine(root, "corrupt.jsonl");
            File.WriteAllBytes(corruptPath, [0x1f, 0x8b, 0x08, 0xff, 0xff]);
            var corrupt = Assert.Throws<SimulationUsageException>(() => SimulationReplayHarness.Replay(new ReplayConfig(corruptPath, RepositoryRoot()), TextWriter.Null));
            Assert.Equal($"record is not valid gzip: {Path.GetFullPath(corruptPath)}", corrupt.Message);
            string completePath = Path.Combine(root, "complete.jsonl.gz");
            Assert.Equal(0, RunTo(completePath));
            byte[] complete = File.ReadAllBytes(completePath);
            using var source = new GZipStream(new MemoryStream(complete), CompressionMode.Decompress);
            using var sourceReader = new StreamReader(source, Encoding.UTF8);
            byte[] changedPayload = Gzip(sourceReader.ReadToEnd(), CompressionLevel.NoCompression);
            int changed = Array.IndexOf(changedPayload, (byte)'{', 10);
            Assert.True(changed > 0);
            changedPayload[changed] = (byte)'[';
            string changedPath = Path.Combine(root, "changed-payload.data");
            File.WriteAllBytes(changedPath, changedPayload);
            var changedError = Assert.Throws<SimulationUsageException>(() => SimulationReportReader.Report(changedPath));
            Assert.Equal($"record is not valid gzip: {Path.GetFullPath(changedPath)}", changedError.Message);
            string truncatedPath = Path.Combine(root, "truncated.data");
            File.WriteAllBytes(truncatedPath, complete[..^8]);
            var truncated = Assert.Throws<SimulationUsageException>(() => SimulationReportReader.Report(truncatedPath));
            Assert.Equal($"record is not valid gzip: {Path.GetFullPath(truncatedPath)}", truncated.Message);
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
        Assert.Equal(0, CommandLine.Run(RunArguments(output: null), output, TextWriter.Null));
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
        var record = RoundTrip(Config(games: 1, seeds: [5], selectionSeed: null, heroes: ["spider_man", "she_hulk"])with { PolicySeed = 9006 });
        var moves = record.Where(item => item.GetProperty("type").GetString() == "step").SelectMany(item => item.GetProperty("events").EnumerateArray()).Where(item => item.GetProperty("kind").GetString() == "CardsMoved").ToList();
        Assert.Contains(moves, moved => moved.GetProperty("to").GetProperty("zone").GetString() == "DealtEncounterCardsDeck" && moved.GetProperty("to").GetProperty("owner").GetInt32() == 1);
        Assert.Contains(moves, moved => moved.GetProperty("to").GetProperty("zone").GetString() == "ObligationsArea" && moved.GetProperty("to").GetProperty("owner").GetInt32() == 0);
    }

    [Fact]
    public void ADecisionLimitWritesAReproductionCapsuleAndMachineSummary()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var diagnostics = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var summary = SimulationRunHarness.Run(Config(games: 1, seeds: [265], selectionSeed: null, decisionLimit: 1), record, diagnostics);
        Assert.Equal(1, summary.ExitCode);
        var documents = Lines(record).ToList();
        var header = Assert.Single(documents, item => item.GetProperty("type").GetString() == "header");
        Assert.Equal(2, header.GetProperty("policy_version").GetInt32());
        var first = Assert.Single(documents, item => item.GetProperty("type").GetString() == "step");
        Assert.False(first.GetProperty("prompt").GetProperty("cancellable").GetBoolean());
        Assert.False(first.GetProperty("decision").GetProperty("decline").GetBoolean());
        var failure = Assert.Single(documents, item => item.GetProperty("type").GetString() == "failure");
        Assert.Equal("decision_limit", failure.GetProperty("category").GetString());
        Assert.Contains("--seed 265", failure.GetProperty("reproduce").GetString());
        Assert.NotNull(failure.GetProperty("last_good_digest").GetString());
        Assert.NotNull(failure.GetProperty("post_failure_digest").GetString());
        var machine = Assert.Single(documents, item => item.GetProperty("type").GetString() == "summary");
        Assert.Equal(1, machine.GetProperty("failures").GetInt32());
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-failure-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, record.ToString());
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
            File.WriteAllLines(path, SchemaTwoLines(record.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)));
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationReportReader.Report(path).Games);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FailedGamesKeepMetricsFromDecisionsThatResolvedBeforeTheLimit()
    {
        var summary = SimulationRunHarness.Run(Config(games: 1, seeds: [265], selectionSeed: null, decisionLimit: 20), TextWriter.Null, TextWriter.Null);
        Assert.Equal(1, summary.Failures);
        Assert.True(summary.CardsPlayed > 0);
        Assert.True(summary.Payments > 0);
    }
}
