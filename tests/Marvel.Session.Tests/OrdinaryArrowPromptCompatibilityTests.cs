using System.Text.Json;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Session.Tests;

/// <summary>Engine choice: replay-v24 compares the exact durable payment shape.</summary>
public sealed class OrdinaryArrowPromptCompatibilityTests
{
    [Theory]
    [InlineData("Y")]
    [InlineData("")]
    public void PredecessorShapedOrdinaryPriceReplaysAndRejectsExplicitComponent(string requirement)
    {
        var price = new CostOption(4, "2", Rule: [requirement]);
        var prompt = PromptFor(price);
        var expected = RoundTrip(prompt);
        Assert.Null(Assert.Single(Assert.Single(expected.Affordances).Costs!).Components);
        JournalReplay.RequirePrompt(expected, prompt, "ordinary arrow");
        Assert.Throws<ReplayDivergenceException>(() => JournalReplay.RequirePrompt(expected,
            PromptFor(price with { Components = [new ResourceCost("2", [requirement])] }), "explicit component"));
    }

    [Fact]
    public void PredecessorShapedCombinedGenericPriceRejectsNullComponentRule()
    {
        var price = new CostOption(4, "5", Rule: ["", ""],
            Components: [new ResourceCost("3"), new ResourceCost("2", [""])]);
        var prompt = PromptFor(price);
        var expected = RoundTrip(prompt);
        JournalReplay.RequirePrompt(expected, prompt, "combined generic arrow");
        Assert.Throws<ReplayDivergenceException>(() => JournalReplay.RequirePrompt(expected,
            PromptFor(price with { Components = [new ResourceCost("3"), new ResourceCost("2")] }),
            "null generic requirement"));
    }

    private static Prompt PromptFor(CostOption price) => new(0, Question.TurnOption,
        TimingPriority.Untimed, "Action", "Synthetic payment", false,
        [new Affordance(7, "Action", 4, 0, "Pay", Costs: [price])]);

    private static PromptRecord RoundTrip(Prompt prompt) => JsonSerializer.Deserialize<PromptRecord>(
        JsonSerializer.Serialize(PromptRecord.From(prompt), JournalJson.Options), JournalJson.Options)!;
}
