using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class IndependentDistinctResourceReviewTests
{
    [Fact]
    public void AllocationWithMixedWildAndPrintedComponentsMustPassAssessment()
    {
        // Synthetic contract: two distinct declared types and one printed mental
        // resource share a source whose two wild icons may declare independently.
        var option = MixedComponents();
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(option, [7]));

        Assert.True(ResourcePayment.Allows(option, [7], null, allocation),
            string.Join("; ", allocation.Select(part => $"{part.Cost}:{part.PaidAs}")));
    }

    [Fact]
    public void PayingPrintedComponentFirstEstablishesThatTheMixedCostIsPayable()
    {
        Assert.True(ResourcePayment.Allows(MixedComponents(), [7], null,
            [new(7, 1, "B"), new(7, 0, "GB")]));
    }

    [Fact]
    public void ExcessGeneratedIconsAreLostAndRepeatedDeclarationsCannotPay()
    {
        var option = new CostOption(0, "2", Sources: [new(7, "YYBG")],
            Components: [new ResourceCost("2") { DistinctTypes = true }]);
        Assert.True(ResourcePayment.Allows(option, [7], null, [new(7, 0, "YB")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "YY")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "YBG")]));
        Assert.Equal("YYBG", Assert.Single(option.Generators).Generates);
    }

    private static CostOption MixedComponents() => new(0, "3", Sources: [new(7, "GGB")],
        Components:
        [
            new ResourceCost("2") { DistinctTypes = true },
            new ResourceCost("1", ["B"], Printed: true),
        ]);
}
