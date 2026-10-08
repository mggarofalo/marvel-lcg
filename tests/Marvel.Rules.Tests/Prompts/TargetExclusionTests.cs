using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class TargetExclusionTests
{
    [Fact]
    public void MutuallyExclusiveCandidatesRemainIndividuallyLegalButCannotBeCombined()
    {
        // This is an engine wire contract, not a new game rule.
        var request = new TargetRequest([1, 2, 3, 4], 1, 3) { ExclusiveSets = [[1, 3], [2, 4]] };
        Assert.True(request.Allows([1]));
        Assert.True(request.Allows([3]));
        Assert.True(request.Allows([3, 2]));
        Assert.False(request.Allows([1, 3]));
        Assert.False(request.Allows([4, 2]));
        Assert.False(request.Allows([1, 2, 3]));
        Assert.False(request.Allows([5]));
        Assert.True(request.AllowsCombination([]));
        Assert.False(request.Allows([]));
    }

    [Fact]
    public void ExclusiveSetsDoNotBecomeAnOrderedCompleteGroupOrDisallowRepeatedAllocationToOneObject()
    {
        var request = new TargetRequest([1, 2, 3], 2, 3, AllowRepeated: true) { ExclusiveSets = [[1, 2]] };
        Assert.False(request.IsGrouped);
        Assert.True(request.Allows([3, 1]));
        Assert.True(request.Allows([1, 3]));
        Assert.True(request.Allows([1, 1]));
        Assert.False(request.Allows([1, 2]));
    }
}
