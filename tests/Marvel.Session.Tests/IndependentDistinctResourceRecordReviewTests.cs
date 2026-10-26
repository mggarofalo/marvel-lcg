using System.Text.Json;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class IndependentDistinctResourceRecordReviewTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("1")]
    [InlineData("\"true\"")]
    public void DistinctnessRequiresABoolean(string value)
    {
        string json = "{\"cost\":\"2\",\"rule\":null,\"printed\":false,\"repeated_resource\":null,\"distinct_types\":"
            + value + "}";
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ResourceCostComponentRecord>(json, SessionSaveJson.Options));
    }

    [Fact]
    public void ReplayComparisonDetectsChangedDistinctnessButAcceptsOrdinaryDefault()
    {
        var ordinary = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Action", "Synthetic payment", false,
            [new Affordance(7, "Action", 4, 0, "Pay", Costs:
                [new CostOption(0, "2", Components: [new ResourceCost("2")])])]);
        var distinct = ordinary with
        {
            Affordances = [ordinary.Affordances[0] with
            {
                Costs = [new CostOption(0, "2", Components:
                    [new ResourceCost("2") { DistinctTypes = true }])],
            }],
        };
        var recorded = PromptRecord.From(ordinary);
        JournalReplay.RequirePrompt(recorded, ordinary, "ordinary");
        Assert.Throws<ReplayDivergenceException>(() =>
            JournalReplay.RequirePrompt(recorded, distinct, "distinct"));
    }
}
