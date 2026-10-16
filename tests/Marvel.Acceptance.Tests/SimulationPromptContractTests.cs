using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SimulationPromptContractTests : SimulationHarnessTestBase
{
    [Fact]
    public void CanonicalPromptRecordsOmitDerivedAliases()
    {
        List<string> current = SuccessfulLines(seed: 1);
        JsonElement[] steps = [.. current.Select(line => JsonSerializer.Deserialize<JsonElement>(line))
            .Where(record => record.GetProperty("type").GetString() == "step")];
        Assert.NotEmpty(steps);
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

    }
}
