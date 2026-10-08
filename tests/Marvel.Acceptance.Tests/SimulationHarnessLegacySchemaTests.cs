using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SimulationHarnessLegacySchemaTests : SimulationHarnessTestBase
{
    [Fact]
    public void CanonicalPromptSchemaShrinksPaymentRecordsAndSchemaTwoStillReplays()
    {
        List<string> current = SuccessfulLines(seed: 1);
        JsonElement[] steps = [.. current.Select(line => JsonSerializer.Deserialize<JsonElement>(line))
            .Where(record => record.GetProperty("type").GetString() == "step")];
        // These historical schemas can represent card anchors only. This
        // completed Core trace offers real payments without area choices.
        Assert.NotEmpty(steps);
        Assert.All(steps.SelectMany(step => step.GetProperty("prompt").GetProperty("affordances").EnumerateArray()),
            offer => Assert.Equal(0, offer.GetProperty("anchor_kind").GetInt32()));
        Assert.Contains(steps, step => step.GetProperty("prompt").GetProperty("affordances").EnumerateArray()
            .Any(offer => offer.GetProperty("costs").GetArrayLength() > 0));
        Assert.Equal(5, JsonNode.Parse(current[0])!["schema"]!.GetValue<int>());
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
        Assert.True(currentBytes * 100 <= legacyBytes * 97, $"schema 5 used {currentBytes} bytes versus schema 2's {legacyBytes}");
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-schema-two-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, legacy);
            Assert.Equal(1, SimulationReplayHarness.Replay(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
            Assert.Equal(1, SimulationReportReader.Report(path).Games);
            File.WriteAllLines(path, SchemaThreeLines(current));
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

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void HistoricalSchemasRepresentCardAnchoredNextMinionChoices(int schema)
    {
        List<string> current = SuccessfulLines();
        JsonElement order = current.Select(line => JsonSerializer.Deserialize<JsonElement>(line))
            .First(record => record.GetProperty("type").GetString() == "step"
                && record.GetProperty("prompt").GetProperty("asking").GetString() == "Order");
        Assert.All(order.GetProperty("prompt").GetProperty("affordances").EnumerateArray(),
            offer => Assert.Equal((int)AffordanceAnchorKind.Card, offer.GetProperty("anchor_kind").GetInt32()));
        List<string> downgraded = schema == 2 ? SchemaTwoLines(current) : SchemaThreeLines(current);
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-area-downgrade-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllLines(path, downgraded);
            Assert.Equal(1, SimulationReplayHarness.Replay(
                new ReplayConfig(path, RepositoryRoot()), TextWriter.Null).Games);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
