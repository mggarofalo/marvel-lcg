using System.Text.Json;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class DistinctResourceCostRecordTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DurableComponentsPreserveDistinctnessAndOmitTheOrdinaryDefault(bool distinct)
    {
        var record = ResourceCostComponentRecord.From(new ResourceCost("2") { DistinctTypes = distinct });
        string json = JsonSerializer.Serialize(record, SessionSaveJson.Options);
        Assert.Equal(distinct, json.Contains("distinct_types", StringComparison.Ordinal));
        Assert.Equal(distinct, JsonSerializer.Deserialize<ResourceCostComponentRecord>(json,
            SessionSaveJson.Options)!.DistinctTypes);
        if (!distinct)
            Assert.Equal("{\"cost\":\"2\",\"rule\":null,\"printed\":false,\"repeated_resource\":null}", json);
    }
}
