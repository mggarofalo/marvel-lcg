using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class PolicyTargetSelectionTests
{
    [Fact]
    public void RepeatedAllocationFillsPastTheDistinctCandidateCountWithinCapacities()
    {
        var request = new TargetRequest([10, 20], 3, 3, AllowRepeated: true,
            MaximumOccurrences: new Dictionary<int, int> { [10] = 2, [20] = 2 });
        Assert.Equal([10, 20, 10], PolicyTargetSelection.Select(request));
    }

    [Fact]
    public void OrdinarySelectionsRemainDistinctAndRespectExclusions()
    {
        var request = new TargetRequest([10, 20, 30], 2, 2) { ExclusiveSets = [[10, 20]] };
        Assert.Equal([10, 30], PolicyTargetSelection.Select(request));
    }

    [Fact]
    public void CompleteGroupsOverridePooledMinimum()
    {
        var request = new TargetRequest([10, 20, 30], 3, 3, Groups: [[10, 20], [10, 30]]);
        Assert.Equal([10, 20], PolicyTargetSelection.Select(request));
    }
}
