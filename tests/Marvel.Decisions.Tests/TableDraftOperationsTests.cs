using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class TableDraftOperationsTests : DecisionComposerTestBase
{
    [Fact]
    public void DuplicateLabelsUseTheirOfferedStableAffordanceIdWithoutSubmitting()
    {
        var composer = new DecisionComposer(Prompt(false,
            new Affordance(14, "Play", 101, 0, "Web-Shooter"),
            new Affordance(29, "Play", 102, 0, "Web-Shooter")));
        var operations = new TableDraftOperations(composer);

        Assert.True(operations.TrySelectAffordance(29));

        Assert.Equal(29, composer.Selected?.Id);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out string? error), error);
        Assert.Equal(29, decision?.Affordance);
    }

    [Fact]
    public void IllegalAndUnregisteredAffordancesCannotChangeTheDraft()
    {
        var composer = new DecisionComposer(Prompt(false,
            new Affordance(14, "Play", 101, 0, "Legal"),
            new Affordance(29, "Play", 102, 0, "Unavailable", Illegal: "Need a resource")));
        var operations = new TableDraftOperations(composer);

        Assert.False(operations.TrySelectAffordance(999));
        Assert.Null(composer.Selected);
        Assert.False(operations.TrySelectAffordance(29));
        Assert.Null(composer.Selected);
        Assert.True(operations.TrySelectAffordance(14));
        Assert.Equal(14, composer.Selected?.Id);
    }

    [Fact]
    public void TargetMutationsRetainSingleOrderedRepeatedGroupedAndSearchShapes()
    {
        var single = Operations(new TargetRequest([1, 2], 1, 1));
        Assert.True(single.Operations.TryAddTarget(1));
        Assert.True(single.Operations.TryAddTarget(2));
        Assert.Equal([2], single.Composer.Targets);

        var ordered = Operations(new TargetRequest([1, 2, 3], 2, 3));
        Assert.True(ordered.Operations.TryAddTarget(2));
        Assert.True(ordered.Operations.TryAddTarget(1));
        Assert.Equal([2, 1], ordered.Composer.Targets);

        var repeated = Operations(new TargetRequest([1, 2], 1, 3,
            AllowRepeated: true, MaximumOccurrences: new Dictionary<int, int> { [1] = 2, [2] = 1 }));
        Assert.True(repeated.Operations.TryAddTarget(1));
        Assert.True(repeated.Operations.TryAddTarget(1));
        Assert.False(repeated.Operations.TryAddTarget(1));
        Assert.True(repeated.Operations.TryAddTarget(2));
        Assert.Equal([1, 1, 2], repeated.Composer.Targets);

        var grouped = Operations(new TargetRequest([1, 2, 3], 3, 3,
            Groups: [[1, 2], [1, 3]]));
        Assert.False(grouped.Operations.TrySelectGroup([2, 1]));
        Assert.True(grouped.Operations.TrySelectGroup([1, 3]));
        Assert.Equal([1, 3], grouped.Composer.Targets);

        var search = Operations(new TargetRequest([4, 5], 1, 1, IsSearch: true));
        Assert.False(search.Operations.TryToggleTarget(99));
        Assert.True(search.Operations.TryToggleTarget(5));
        Assert.Equal([5], search.Composer.Targets);
    }

    [Fact]
    public void GeneratorMustBeOfferedByTheSelectedApplicableCost()
    {
        CostOption first = new(11, "1", Sources: [new ResourceSource(41, "Y")]);
        CostOption second = new(12, "1", Sources: [new ResourceSource(42, "R")]);
        var composer = new DecisionComposer(Prompt(false,
            new Affordance(14, "Play", 101, 0, "Choice",
                new TargetRequest([11, 12], 1, 1), [first, second])));
        var operations = new TableDraftOperations(composer);
        Assert.True(operations.TrySelectAffordance(14));
        Assert.True(operations.TryToggleTarget(11));
        Assert.True(operations.TrySelectCost(0));

        Assert.False(operations.TryToggleGenerator(42));
        Assert.True(operations.TryToggleGenerator(41));
        Assert.Equal([41], composer.Resources);
    }

    private static (TableDraftOperations Operations, DecisionComposer Composer) Operations(TargetRequest request)
    {
        var composer = new DecisionComposer(Prompt(false,
            new Affordance(14, "Choose", 101, 0, "Choice", request)));
        var operations = new TableDraftOperations(composer);
        Assert.True(operations.TrySelectAffordance(14));
        return (operations, composer);
    }
}
