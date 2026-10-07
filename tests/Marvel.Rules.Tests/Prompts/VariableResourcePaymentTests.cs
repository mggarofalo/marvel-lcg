using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class VariableResourcePaymentTests
{
    [Rule("rr:non-numerical-variable.1")]
    [Fact]
    public void EveryChosenVariableSlotRetainsItsResourceType()
    {
        // "For costs involving the letter X, the value of X is defined by card
        // ability or player choice"; defining two typed slots does not make one generic.
        var cost = EnergyCost();
        var values = new Dictionary<string, long> { ["X"] = 2 };

        Assert.Equal("YY", ResourcePayment.RequiredResources(cost.ResourceCosts[0], values));
        Assert.True(ResourcePayment.CanContribute(cost, 1, values));
        Assert.False(ResourcePayment.CanContribute(cost, 2, values));
        Assert.Null(ResourcePayment.Allocate(cost, [1, 2], values));
        Assert.False(ResourcePayment.Allows(cost, [1, 2], values,
            [new ResourceAllocation(1, 0, "Y"), new ResourceAllocation(2, 0, "B")]));
        Assert.Null(ResourcePayment.Allocate(cost, [1], values));
    }

    [Rule("rr:wild-resource.1")]
    [Fact]
    public void WildPaysTheRepeatedTypeWithAnExplicitMatchingDeclaration()
    {
        // "When a player generates a wild resource ([wild]), they may specify
        // which resource type (energy, mental, physical, or wild) it is being used as."
        var cost = EnergyCost();
        var values = new Dictionary<string, long> { ["X"] = 2 };

        Assert.True(ResourcePayment.CanContribute(cost, 3, values));
        var allocation = ResourcePayment.Allocate(cost, [1, 3], values);
        Assert.NotNull(allocation);
        Assert.All(allocation, icon => Assert.Equal("Y", icon.PaidAs));
        Assert.True(ResourcePayment.Allows(cost, [1, 3], values, allocation));
        Assert.False(ResourcePayment.Allows(cost, [1, 3], values,
            [new ResourceAllocation(1, 0, "Y"), new ResourceAllocation(3, 0, "B")]));
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "Y")]
    [InlineData(3, "YYY")]
    public void RequiredIconsFollowTheDefinedQuantity(long amount, string expected)
    {
        var cost = EnergyCost().ResourceCosts[0];
        Assert.Equal(expected, ResourcePayment.RequiredResources(cost, new Dictionary<string, long> { ["X"] = amount }));
        Assert.Null(ResourcePayment.RequiredResources(cost));
    }

    [Fact]
    public void ConflictingOrInvalidRequirementShapesCannotAllocate()
    {
        var cost = EnergyCost();
        var values = new Dictionary<string, long> { ["X"] = 2 };
        Assert.Null(ResourcePayment.Allocate(cost with
        {
            Components = [cost.ResourceCosts[0] with { Rule = ["Y"] }],
        }, [1, 3], values));
        Assert.Null(ResourcePayment.Allocate(cost with
        {
            Components = [cost.ResourceCosts[0] with { RepeatedResource = '?' }],
        }, [1, 3], values));
    }

    private static CostOption EnergyCost() => new(0, "X",
        Sources: [new ResourceSource(1, "Y"), new ResourceSource(2, "B"), new ResourceSource(3, "G")],
        Variables: [new VariableRequest("X", 1, 3)],
        Components: [new ResourceCost("X") { RepeatedResource = Resources.Energy }]);
}
