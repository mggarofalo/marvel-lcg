using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class VariablePaymentDraftTests : DecisionComposerTestBase
{
    [Fact]
    public void IncreasingTypedVariableCannotTurnAnUnrelatedIconIntoAValidPayment()
    {
        var cost = new CostOption(0, "X",
            Sources: [new ResourceSource(1, "Y"), new ResourceSource(2, "B"), new ResourceSource(3, "G")],
            Variables: [new VariableRequest("X", 1, 2)],
            Components: [new ResourceCost("X") { RepeatedResource = Resources.Energy }]);
        var composer = new DecisionComposer(Prompt(false, new Affordance(7, "Ability", 20, 0, "Variable payment", Costs: [cost])));
        composer.SelectAffordance(7);
        composer.Define("X", 1);
        composer.ToggleResource(1);
        Assert.True(composer.TryBuild(out _, out _));
        composer.Define("X", 2);
        Assert.False(composer.TryBuild(out _, out _));
        Assert.False(DecisionResourceEligibility.CanToggle(composer, 2));
        composer.ToggleResource(2);
        Assert.False(composer.TryBuild(out _, out _));
        Assert.True(DecisionResourceEligibility.CanToggle(composer, 2));
        composer.ToggleResource(2);
        Assert.True(DecisionResourceEligibility.CanToggle(composer, 3));
        composer.ToggleResource(3);
        Assert.True(composer.TryBuild(out var answer, out var error), error);
        Assert.All(answer!.Allocations!, icon => Assert.Equal("Y", icon.PaidAs));
        Assert.Equal(2, answer.Values!["X"]);
    }
}
