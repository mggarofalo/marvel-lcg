using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class ConditionalResourceComposerReviewTests : DecisionComposerTestBase
{
    [Fact]
    public void ConditionalDraftAndEngineAgreeAcrossQualifyingAndNonqualifyingAssignments()
    {
        var source = new ResourceSource(40, "B") { ConditionalBonus = new("Y", 1) };
        var cost = new CostOption(20, "2", Sources: [source, new ResourceSource(41, "B")],
            Components: [new ResourceCost("1", ["B"]), new ResourceCost("1", ["Y"])]);
        var composer = new DecisionComposer(Prompt(cancellable: false,
            new Affordance(7, "Play", 20, 0, "Synthetic", Costs: [cost])));
        composer.SelectAffordance(7);
        composer.ToggleResource(40);
        composer.ToggleResource(41);
        composer.AssignResource(41, 0, 0, Resources.Mental);
        composer.AssignResource(40, 1, 1, Resources.Energy);
        Assert.Equal(2, composer.Progress().Payment.GeneratedIcons);
        Assert.False(composer.Progress().IsReady);
        Assert.False(composer.TryBuild(out _, out _));
        Assert.False(ResourcePayment.Allows(cost, [40, 41], null,
            [new(41, 0, "B"), new(40, 1, "Y")]));
        composer.AssignResource(40, 1, null, null);
        composer.AssignResource(41, 0, null, null);
        composer.AssignResource(40, 0, 0, Resources.Mental);
        composer.AssignResource(40, 1, 1, Resources.Energy);
        Assert.Equal(3, composer.Progress().Payment.GeneratedIcons);
        Assert.True(composer.Progress().IsReady);
        Assert.True(composer.TryBuild(out var decision, out _));
        Assert.True(ResourcePayment.Allows(cost, decision!.Spent, decision.Values, decision.Allocated));
    }
}

