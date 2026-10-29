using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SupportActionCopyReviewTests
{
    [Theory]
    [InlineData(null, "Prepared source (copy 2 of 2)")]
    [InlineData("", "Prepared source (copy 2 of 2)")]
    [InlineData("Discard the supplied source", "Prepared source (copy 2 of 2) · Discard the supplied source")]
    public void SourceNamedChoiceRetainsPhysicalCopyAndOnlySuppliedCost(string? cost, string expected)
    {
        var offer = new AffordancePresentation(1, "wire_label", null, "Action", "Prepared source", 8, 0, null, "", [])
        {
            SourceName = "Prepared source (copy 2 of 2)",
            DisplayLabel = "Prepared source",
            CostDescription = cost,
        };

        Assert.Equal(expected, DecisionCopy.Choice(offer));
    }

    [Fact]
    public void MatchingPlayExplainsBothExhaustionAndEffectBeforeCommitment()
    {
        var offer = new AffordancePresentation(1, "12024", "Play a card from your hand that shares a trait with your hero, reducing its cost by 1",
            "Action", "Team-Building Exercise", 8, 0, null, "", [])
        { SourceName = "Team-Building Exercise", DisplayLabel = "Play a matching card", CostDescription = "Exhaust Team-Building Exercise" };
        Assert.Equal("Play a matching card  ·  Team-Building Exercise", DecisionCopy.Choice(offer));
        Assert.Contains(offer.CostDescription!, DecisionCopy.ActionSummary(offer));
        Assert.Contains(offer.Description!, DecisionCopy.ActionSummary(offer));
    }

    [Fact]
    public void CostDoesNotReplacePlayIntentOrExplicitActionMeaning()
    {
        var offer = new AffordancePresentation(1, "Prepared source", null, "Action", "Prepared source", 8, 0, null, "", [])
        {
            CostDescription = "Exhaust supplied source",
            Description = "Supplied effect remains distinct from a cost",
        };

        Assert.Equal("Play Prepared source", DecisionCopy.Choice(offer with { PlaysCard = true }));
        Assert.Equal("Ready your hero  ·  Prepared source", DecisionCopy.Choice(offer with { DisplayLabel = "Ready your hero" }));
        Assert.Equal("Use Prepared source", DecisionCopy.Choice(offer with { Label = "Action" }));
        Assert.Contains(offer.Description, DecisionCopy.ActionSummary(offer));
        Assert.Contains(offer.CostDescription, DecisionCopy.ActionSummary(offer));
    }
}
