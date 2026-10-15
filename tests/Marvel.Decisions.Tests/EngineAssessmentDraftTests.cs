using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class EngineAssessmentDraftTests : DecisionComposerTestBase
{
    [Fact]
    public void RepeatedSelectionCannotStageAnExcludedTargetOrExceedCapacity()
    {
        // Synthetic composition contract: repeated allocations with exclusive candidates.
        var request = new TargetRequest([1, 2, 3], 2, 3, AllowRepeated: true,
            MaximumOccurrences: new Dictionary<int, int> { [1] = 2, [2] = 2, [3] = 1 })
        { ExclusiveSets = [[1, 2]] };
        var composer = new DecisionComposer(Prompt(false, new Affordance(7, "Choose", 0, 0, "Allocate", request)));
        var operations = new TableDraftOperations(composer);
        Assert.True(operations.TrySelectAffordance(7));
        Assert.True(operations.TryAddTarget(1));
        Assert.False(composer.TryBuild(out _, out _));
        Assert.False(operations.CanAddTarget(2));
        Assert.False(operations.TryAddTarget(2));
        Assert.True(operations.TryAddTarget(1));
        Assert.False(operations.CanAddTarget(1));
        Assert.False(operations.TryAddTarget(1));
        Assert.True(operations.TryAddTarget(3));
        Assert.True(composer.TryBuild(out var answer, out _));
        Assert.Equal([1, 1, 3], answer!.Targets);
    }

    [Fact]
    public void PrintedComponentDoesNotOfferOrAcceptWildSubstitution()
    {
        // Synthetic simultaneous payment; only the second component permits substitution.
        var cost = new CostOption(0, "2", Sources: [new ResourceSource(7, "GG")],
            Components: [new ResourceCost("1", Printed: true), new ResourceCost("1", ["R"])],
            DeclarationSensitive: true);
        var composer = new DecisionComposer(Prompt(false, new Affordance(1, "Pay", 0, 0, "Pay", Costs: [cost])));
        composer.SelectAffordance(1);
        Assert.Empty(DecisionResourceDeclarations.For(composer, 7, 0, 0));
        composer.ToggleResource(7);
        Assert.Equal(['G'], DecisionResourceDeclarations.For(composer, 7, 0, 0));
        Assert.Contains('R', DecisionResourceDeclarations.For(composer, 7, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => composer.AssignResource(7, 0, 0, 'R'));
        Assert.Empty(composer.Assignments);
        composer.AssignResource(7, 0, 0, 'G');
        composer.AssignResource(7, 1, 1, 'R');
        Assert.True(composer.TryBuild(out var answer, out string? error), error);
        Assert.NotNull(answer);
        Assert.Empty(DecisionResourceDeclarations.For(composer, 7, 2, 0));
        Assert.Empty(DecisionResourceDeclarations.For(composer, 7, 0, 2));
        Assert.Empty(DecisionResourceDeclarations.For(composer, 99, 0, 0));
    }
}
