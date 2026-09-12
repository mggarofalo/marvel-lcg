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
public abstract class SimulationHarnessTestBase
{
    protected static int RunTo(string path) => CommandLine.Run(RunArguments(path), TextWriter.Null, TextWriter.Null);
    protected static byte[] Gzip(string content, CompressionLevel level)
    {
        var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, level, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes(content));
        }

        return compressed.ToArray();
    }

    protected static byte[] WithFileName(byte[] gzip, string name)
    {
        byte[] header = gzip[..10];
        header[3] |= 0x08;
        return[..header, ..Encoding.Latin1.GetBytes(name), 0, ..gzip[10..]];
    }

    protected static string[] RunArguments(string? output)
    {
        var args = new List<string>
        {
            "run",
            "--scenario",
            "rhino",
            "--difficulty",
            "standard",
            "--hero",
            "spider_man",
            "--modular",
            "bomb_scare",
            "--seed",
            "265",
            "--policy-seed",
            "9001",
            "--decision-limit",
            "800",
            "--repo-root",
            RepositoryRoot(),
        };
        if (output is not null)
        {
            args.Add("--output");
            args.Add(output);
        }

        return[..args];
    }

    private protected static List<JsonElement> RoundTrip(SimulationConfig config)
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var diagnostics = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var summary = SimulationRunHarness.Run(config, record, diagnostics);
        Assert.Equal(0, summary.ExitCode);
        Assert.Contains("\"type\":\"result\"", record.ToString());
        Assert.Contains("\"type\":\"summary\"", record.ToString());
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, record.ToString());
            var replay = SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), diagnostics);
            Assert.Equal(1, replay.Games);
            Assert.True(replay.Steps > 0);
            Assert.Equal(1, SimulationReportReader.Report(path).Games);
            return Lines(record).ToList();
        }
        finally
        {
            File.Delete(path);
        }
    }

    protected static void AssertReplayDiverges(IReadOnlyList<string> lines)
    {
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

    protected static List<string> SuccessfulLines()
    {
        var record = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        SimulationRunHarness.Run(Config(games: 1, seeds: [265], selectionSeed: null), record, TextWriter.Null);
        return record.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    protected static List<string> SchemaTwoLines(IEnumerable<string> current)
    {
        var legacy = new List<string>();
        foreach (string line in current)
        {
            JsonObject record = Assert.IsType<JsonObject>(JsonNode.Parse(line));
            string type = record["type"]!.GetValue<string>();
            if (type == "header")
            {
                record["schema"] = 2;
            }
            else if (type is "step" or "failure")
            {
                AddSchemaTwoPrompt(record);
                if (record["recent_steps"] is JsonArray recent)
                {
                    foreach (JsonNode? recentStep in recent)
                    {
                        AddSchemaTwoPrompt(recentStep!.AsObject());
                    }
                }
            }

            legacy.Add(record.ToJsonString(RecordJson.Options));
        }

        return legacy;
    }

    protected static IEnumerable<string> OldestSchemaTwoLines(IEnumerable<string> legacy)
    {
        foreach (string line in legacy)
        {
            JsonObject record = Assert.IsType<JsonObject>(JsonNode.Parse(line));
            RemoveLaterSchemaTwoPromptFields(record);
            if (record["recent_steps"] is JsonArray recent)
            {
                foreach (JsonNode? recentStep in recent)
                {
                    RemoveLaterSchemaTwoPromptFields(recentStep!.AsObject());
                }
            }

            yield return record.ToJsonString(RecordJson.Options);
        }
    }

    protected static void RemoveLaterSchemaTwoPromptFields(JsonObject record)
    {
        if (record["prompt"] is not JsonObject prompt)
        {
            return;
        }

        foreach (JsonNode? affordanceNode in prompt["affordances"]!.AsArray())
        {
            JsonObject affordance = affordanceNode!.AsObject();
            if (affordance["targets"] is JsonObject target)
            {
                _ = target.Remove("allow_repeated");
                _ = target.Remove("maximum_occurrences");
                _ = target.Remove("details");
            }

            foreach (JsonNode? costNode in affordance["costs"]!.AsArray())
            {
                _ = costNode!.AsObject().Remove("declaration_sensitive");
            }
        }
    }

    protected static void AddSchemaTwoPrompt(JsonObject record)
    {
        if (record["prompt"] is not JsonObject prompt)
        {
            return;
        }

        foreach (JsonNode? affordanceNode in prompt["affordances"]!.AsArray())
        {
            AddSchemaTwoAffordance(affordanceNode!.AsObject());
        }
    }

    protected static void AddSchemaTwoAffordance(JsonObject affordance)
    {
        if (affordance["targets"] is JsonObject target)
            target["is_grouped"] = target["groups"] is JsonArray { Count: > 0 };
        foreach (JsonNode? cost in affordance["costs"]!.AsArray())
            AddSchemaTwoCost(cost!.AsObject());
    }

    protected static void AddSchemaTwoCost(JsonObject cost)
    {
        cost["has_alternative"] = cost["or_cost"]!.GetValue<string>().Length > 0;
        cost["generators"] = cost["sources"]?.DeepClone() ?? new JsonArray();
        cost["variable_requests"] = cost["variables"]?.DeepClone() ?? new JsonArray();
        cost["resource_costs"] = cost["components"]?.DeepClone() ?? new JsonArray(new JsonObject { ["cost"] = cost["cost"]!.GetValue<string>(), ["rule"] = cost["rule"]?.DeepClone(), ["printed"] = false, });
    }

    protected static IEnumerable<JsonElement> Lines(StringWriter writer)
    {
        foreach (string line in writer.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            using var document = JsonDocument.Parse(line);
            yield return document.RootElement.Clone();
        }
    }

    protected static Prompt Prompt(params Affordance[] options) => new(0, Question.TurnOption, TimingPriority.Untimed, "WhenPlayerInTurn", "turn", true, options);
    private protected static SimulationConfig Config(int games, IReadOnlyList<uint> seeds, uint? selectionSeed, IReadOnlyList<string>? heroes = null, uint? seedStart = null, int decisionLimit = 800) => new("rhino", "standard", heroes ?? ["spider_man"], ["bomb_scare"], games, seeds, seedStart, selectionSeed, 9001, decisionLimit, null, RepositoryRoot());
    protected static string RepositoryRoot()
    {
        for (DirectoryInfo? directory = new(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Marvel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("test could not find Marvel.slnx");
    }
}
