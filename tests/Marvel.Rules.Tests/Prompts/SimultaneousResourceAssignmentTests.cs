using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class SimultaneousResourceAssignmentTests
{
    [Theory]
    [InlineData("GGB", false, false)]
    [InlineData("GBG", false, false)]
    [InlineData("BGG", false, false)]
    [InlineData("GGB", true, false)]
    [InlineData("GBG", true, false)]
    [InlineData("BGG", true, false)]
    [InlineData("GGB", false, true)]
    [InlineData("GBG", false, true)]
    [InlineData("BGG", false, true)]
    [InlineData("GGB", true, true)]
    [InlineData("GBG", true, true)]
    [InlineData("BGG", true, true)]
    public void PhysicalAssignmentIsIndependentOfComponentAndAllocationOrder(
        string icons, bool reverseComponents, bool distinct)
    {
        // Synthetic simultaneous contract: one component needs printed mental;
        // the other permits wild declarations, optionally of distinct types.
        ResourceCost[] components = [new("2") { DistinctTypes = distinct }, new("1", ["B"], Printed: true)];
        if (reverseComponents) Array.Reverse(components);
        var option = new CostOption(0, "3", Sources: [new(7, icons)], Components: components);
        int declaredCost = reverseComponents ? 1 : 0;
        int printedCost = reverseComponents ? 0 : 1;
        ResourceAllocation[] explicitPayment = [new(7, declaredCost, "GB"), new(7, printedCost, "B")];

        Assert.True(ResourcePayment.Allows(option, [7], null, explicitPayment));
        Assert.True(ResourcePayment.Allows(option, [7], null, explicitPayment.AsEnumerable().Reverse().ToArray()));
        Assert.True(ResourcePayment.Allows(option, [7], null,
            [new(7, declaredCost, "BG"), new(7, printedCost, "B")]));
        var allocated = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(ResourcePayment.Allocate(option, [7]));
        Assert.True(ResourcePayment.Allows(option, [7], null, allocated));
        Assert.Equal(allocated, ResourcePayment.Allocate(option, [7]));
        Assert.Equal(icons, Assert.Single(option.Generators).Generates);
        Assert.Equal("GB", explicitPayment[0].PaidAs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AssignmentCannotBorrowAnIconFromAnotherNamedSource(bool reverse)
    {
        var option = new CostOption(0, "2", Sources: [new(7, "B"), new(8, "Y")],
            Components: [new ResourceCost("2") { DistinctTypes = true }]);
        ResourceAllocation[] valid = [new(7, 0, "B"), new(8, 0, "Y")];
        ResourceAllocation[] swapped = [new(7, 0, "Y"), new(8, 0, "B")];
        if (reverse)
        {
            Array.Reverse(valid);
            Array.Reverse(swapped);
        }

        Assert.False(ResourcePayment.Allows(option, [7, 8], null, swapped));
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, valid));
    }

    [Fact]
    public void PhysicalAssignmentRejectsUnknownDuplicateAndOverspentResources()
    {
        var option = new CostOption(0, "3", Sources: [new(7, "GGB")],
            Components: [new ResourceCost("2") { DistinctTypes = true }, new("1", ["B"], Printed: true)]);
        ResourceAllocation[] valid = [new(7, 0, "GB"), new(7, 1, "B")];

        Assert.False(ResourcePayment.Allows(option, [7, 7], null, valid));
        Assert.Null(ResourcePayment.Allocate(option, [7, 7]));
        Assert.False(ResourcePayment.Allows(option, [8], null, valid));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(8, 0, "GB"), new(7, 1, "B")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "GBY"), new(7, 1, "B")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "GG"), new(7, 1, "B")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "G?"), new(7, 1, "B")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "GB"), new(7, 2, "B")]));
        Assert.False(ResourcePayment.Allows(option, [7], null, [new(7, 0, "GB"), new(7, 1, "")]));
        Assert.True(ResourcePayment.Allows(option, [7], null, valid));
    }
}
