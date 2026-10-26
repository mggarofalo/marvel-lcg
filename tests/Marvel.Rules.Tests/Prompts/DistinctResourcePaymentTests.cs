using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class DistinctResourcePaymentTests
{
    [Theory]
    [InlineData("YY", false)]
    [InlineData("BB", false)]
    [InlineData("RY", true)]
    [InlineData("BY", true)]
    [InlineData("GY", true)]
    [InlineData("GG", true)]
    [InlineData("YYB", true)]
    public void AllocationRequiresDifferentTypesRatherThanDifferentSources(string icons, bool pays)
    {
        // Synthetic cost contract for printed 05002: "spend 2 resources of different types".
        var cost = Price(icons);
        var allocation = ResourcePayment.Allocate(cost, [7]);
        Assert.Equal(pays, allocation is not null);
        if (allocation is null) return;
        Assert.True(ResourcePayment.Allows(cost, [7], null, allocation));
        Assert.Equal(2, string.Concat(allocation.Select(part => part.PaidAs)).Distinct().Count());
        Assert.Null(ResourcePaymentProgress.RemainingRequired(cost, [7]));
    }

    [Rule("rr:wild-resource.1")]
    [Rule("rr:wild-resource.1.1")]
    [Theory]
    [InlineData("GG", false)]
    [InlineData("YY", false)]
    [InlineData("GY", true)]
    [InlineData("RB", true)]
    public void DoubleWildsAreIndependentlyDeclaredAndInvalidRetryDoesNotChangeTheOffer(string declared, bool pays)
    {
        // "Each of its wild resources can be declared a different type."
        var cost = Price("GG");
        Assert.Equal(pays, ResourcePayment.Allows(cost, [7], null, [new(7, 0, declared)]));
        Assert.True(ResourcePayment.Allows(cost, [7], null, [new(7, 0, "YB")]));
        Assert.Equal("GG", Assert.Single(cost.Generators).Generates);
    }

    [Fact]
    public void SeparateSameTypeSourcesCannotPayAndDistinctnessIsLocalToTheComponent()
    {
        var cost = Price("Y") with { Sources = [new(7, "Y"), new(8, "Y")] };
        Assert.Null(ResourcePayment.Allocate(cost, [7, 8]));
        Assert.False(ResourcePayment.Allows(cost, [7, 8], null, [new(7, 0, "Y"), new(8, 0, "Y")]));
        var combined = Price("YBY") with
        {
            Cost = "3", Components = [new ResourceCost("2") { DistinctTypes = true }, new("1")],
        };
        var allocated = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(combined, [7]));
        Assert.True(ResourcePayment.Allows(combined, [7], null, allocated));
    }

    [Fact]
    public void PrintedAndFixedRequirementsRemainBinding()
    {
        var cost = Price("GG") with
        {
            Components = [new ResourceCost("2", Printed: true) { DistinctTypes = true }],
        };
        Assert.Null(ResourcePayment.Allocate(cost, [7]));
        Assert.False(ResourcePayment.Allows(cost, [7], null, [new(7, 0, "YB")]));
        var fixedCost = Price("GG") with
        {
            Components = [new ResourceCost("2", ["Y"]) { DistinctTypes = true }],
        };
        Assert.False(ResourcePayment.Allows(fixedCost, [7], null, [new(7, 0, "RB")]));
        Assert.True(ResourcePayment.Allows(fixedCost, [7], null, [new(7, 0, "YB")]));
    }

    [Theory]
    [InlineData("5", "")]
    [InlineData("2", "YY")]
    public void ImpossibleDistinctRequirementsHaveNoAllocation(string amount, string required)
    {
        var cost = Price("GGGGG") with
        {
            Components = [new ResourceCost(amount, [required]) { DistinctTypes = true }],
        };
        Assert.Null(ResourcePayment.Allocate(cost, [7]));
        Assert.False(ResourcePayment.Allows(cost, [7], null, [new(7, 0, "YBRGG")]));
    }

    private static CostOption Price(string icons) => new(0, "2", Sources: [new(7, icons)],
        Components: [new ResourceCost("2") { DistinctTypes = true }]);
}
