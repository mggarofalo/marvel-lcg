using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class TypedPaymentProgressTests
{
    [Fact]
    public void DefinedXChangesComponentProgressWithoutDeclaringOrCommittingPayment()
    {
        var cost = new CostOption(0, "1+X", Sources: [new(7, "YY"), new(8, "BB")],
            Variables: [new("X", 0, 5)], Components: [new("1"), new ResourceCost("X") { RepeatedResource = 'Y' }]);
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "synthetic", "Choose", false, [new Affordance(3, "Play", 19, 0, "Card", Costs: [cost])]));
        composer.SelectAffordance(3);
        Assert.Null(composer.Progress().Payment.Components);
        composer.Define("X", 2);
        Assert.Equal([1, 2], composer.Progress().Payment.Components!.Select(part => part.Remaining));
        composer.ToggleResource(7);
        Assert.Equal([1, 0], composer.Progress().Payment.Components!.Select(part => part.Remaining));
        Assert.False(composer.Progress().IsReady);
        composer.ToggleResource(8);
        Assert.Equal([0, 0], composer.Progress().Payment.Components!.Select(part => part.Remaining));
        Assert.True(composer.Progress().Payment.CanCoverCost);
        Assert.False(composer.Progress().IsReady);
        composer.AssignResource(7, 0, 0, 'Y');
        composer.AssignResource(7, 1, 1, 'Y');
        composer.AssignResource(8, 0, 1, 'B');
        Assert.Equal([0, 0], composer.Progress().Payment.Components!.Select(part => part.Remaining));
        Assert.False(composer.Progress().IsReady);
        composer.AssignResource(7, 0, 1, 'Y');
        composer.AssignResource(8, 0, 0, 'B');
        Assert.True(composer.Progress().IsReady);
        composer.Define("X", 3);
        Assert.Equal([0, 1], composer.Progress().Payment.Components!.Select(part => part.Remaining));
        Assert.False(composer.Progress().Payment.CanCoverCost);
    }
}
