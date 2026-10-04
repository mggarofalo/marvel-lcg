using Marvel.Decisions;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionCopyTests
{
    [Theory]
    [InlineData("Choose option", "Choose Rhino", "Choose Rhino")]
    [InlineData("Choose", "Choose Black Cat", "Choose Black Cat")]
    [InlineData("Choose option", "Rhino", "Choose Rhino")]
    [InlineData("Choose_Option", "Rhino", "Choose Rhino")]
    [InlineData("Attack", "Do something else", "Attack")]
    public void StructuredChoiceCommitUsesTheOfferedChoiceName(string verb, string label, string expected) =>
        Assert.Equal(expected, DecisionCopy.GenericCommit(verb, label, "Other card"));

    [Fact]
    public void CardActionsUseOneReadableName()
    {
        var action = new AffordancePresentation(
            1,
            "First Aid",
            null,
            "Action",
            "First Aid",
            40,
            0,
            null,
            "No selection",
            []);

        Assert.Equal("First Aid", DecisionCopy.Choice(action));
        Assert.Equal("Use First Aid", DecisionCopy.GenericCommit(
            action.Verb, action.Label, action.Anchor));
    }

    [Fact]
    public void WireIdentifiersBecomeReadableActionLabels()
    {
        var action = new AffordancePresentation(
            1,
            "Change_Form",
            null,
            "Change Form",
            "Spider-Man",
            1,
            0,
            null,
            "No selection",
            []);

        Assert.Equal("Change Form  ·  Spider-Man", DecisionCopy.Choice(action));
    }

    [Fact]
    public void StickyActionSummaryCarriesTheCurrentCostConsequence()
    {
        var action = new AffordancePresentation(
            1,
            "Play",
            null,
            "Play",
            "Web-Shooter",
            1,
            0,
            null,
            "No selection",
            ["Cost 0 · 5 generators"],
            "Current cost 0; printed cost 1.");

        Assert.Equal(
            "Play Web-Shooter\nCurrent cost 0; printed cost 1.",
            DecisionCopy.ActionSummary(action));
    }

    [Fact]
    public void NamedCopiesRemainDistinctWhileTheirCostsStateAndEffectStayReadable()
    {
        var action = new AffordancePresentation(1, "Action", "Remove 1 threat", "Action",
            "Surveillance Team", 7, 0, null, "", [])
        {
            SourceName = "Surveillance Team (copy 2 of 2)",
            SourceState = "Ready · 3 snoop counters",
            CostDescription = "Exhaust Surveillance Team; Remove 1 snoop counter",
        };

        Assert.Equal("Use Surveillance Team (copy 2 of 2)", DecisionCopy.Choice(action));
        Assert.Contains("3 snoop counters", DecisionCopy.ActionSummary(action));
        Assert.Contains("Costs: Exhaust Surveillance Team; Remove 1 snoop counter", DecisionCopy.ActionSummary(action));
        Assert.EndsWith("Remove 1 threat", DecisionCopy.ActionSummary(action));
    }

    [Fact]
    public void PlayingAnActionEventUsesTheEnginePlayIntentRegardlessOfItsAbilityLabel()
    {
        var action = new AffordancePresentation(1, "Swinging Web Kick", "Attack an enemy", "Action",
            "Swinging Web Kick", 7, 0, null, "", []) { PlaysCard = true };

        Assert.Equal("Play Swinging Web Kick", DecisionCopy.Choice(action));
    }

    [Fact]
    public void AChoiceWhoseWireLabelWasRemovedUsesTheReadableCardName()
    {
        var action = new AffordancePresentation(
            1, "Choose", null, "Choose", "Repulsor Blast", 40, 0, null,
            "No selection", []);

        Assert.Equal("Choose Repulsor Blast", DecisionCopy.Choice(action));
    }

    [Theory]
    [InlineData(1, "Play Web-Shooter  ·  Lose 1 excess resource")]
    [InlineData(2, "Play Web-Shooter  ·  Lose 2 excess resources")]
    public void CommitButtonNamesTheResourcesThatWillBeLost(int excess, string expected)
    {
        var payment = new PaymentProgress(
            CostSelectionState.Selected,
            0,
            1,
            1,
            excess,
            0,
            0,
            0,
            true);

        Assert.Equal(expected, DecisionCopy.WithPaymentConsequence(
            "Play Web-Shooter", payment));
        Assert.Contains("will be lost", DecisionCopy.OverpaymentWarning(payment));
    }
}
