using Marvel.Decisions;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionCopyTests
{
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
