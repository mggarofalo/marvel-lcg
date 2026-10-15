using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class PartialTargetSelectionTests
{
    [Fact]
    public void EmptyOrdinaryOffersPermitOnlyTheEmptyDraftAndStillRequireTheirMinimum()
    {
        var request = new TargetRequest([], 1, 1);
        Assert.True(request.AllowsPartial([]));
        Assert.False(request.Allows([]));
        Assert.False(request.AllowsPartial([1]));
        Assert.True((request with { Min = 0 }).Allows([]));
    }

    [Fact]
    public void PartialAllocationRetainsExclusionsAndCapacitiesBeforeCompletion()
    {
        // Synthetic offer: exclusions and capacities are engine-authored contract facts.
        var request = new TargetRequest([1, 2, 3], 3, 3, AllowRepeated: true,
            MaximumOccurrences: new Dictionary<int, int> { [1] = 2, [2] = 2 })
        { ExclusiveSets = [[1, 2]] };
        Assert.True(request.AllowsPartial([1]));
        Assert.False(request.Allows([1]));
        Assert.True(request.AllowsPartial([1, 1]));
        Assert.False(request.AllowsPartial([1, 1, 1]));
        Assert.False(request.AllowsPartial([1, 2]));
        Assert.False(request.AllowsPartial([3]));
        Assert.False(request.AllowsPartial([99]));
    }

    [Rule("rr:choose-game-element.3.1")]
    [Fact]
    public void PartialOrdinarySelectionRejectsRepeatedTargetsAndRetainsMinimum()
    {
        // "The same target cannot be chosen multiple times this way."
        var request = new TargetRequest([1, 2, 3], 2, 2);
        Assert.True(request.AllowsPartial([1]));
        Assert.False(request.Allows([1]));
        Assert.False(request.AllowsPartial([1, 1]));
        Assert.True(request.Allows([1, 2]));
        Assert.False(request.AllowsPartial([1, 2, 3]));
    }

    [Fact]
    public void GroupDraftUsesOfferedOrderAndIgnoresPooledCount()
    {
        // Prefix composition is an engine contract choice, not a tabletop rule.
        var request = new TargetRequest([1, 2, 3], 3, 3, Groups: [[1, 2], [3]]);
        Assert.True(request.AllowsPartial([]));
        Assert.True(request.AllowsPartial([1]));
        Assert.False(request.Allows([1]));
        Assert.False(request.AllowsPartial([2]));
        Assert.False(request.AllowsPartial([99]));
        Assert.False(request.AllowsPartial([2, 1]));
        Assert.True(request.Allows([1, 2]));
        Assert.True(request.Allows([3]));
    }
}
