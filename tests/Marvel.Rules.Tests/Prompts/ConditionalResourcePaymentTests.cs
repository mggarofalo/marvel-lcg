using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class ConditionalResourcePaymentTests
{
    private static readonly ResourceSource Conditional = new(7, "G")
    { ConditionalBonus = new("G", 3) };

    [Fact]
    public void ZeroCostAndWhollyExcessSourceProduceOnlyBase()
    {
        var zero = new CostOption(0, "0", Sources: [Conditional]);
        Assert.True(ResourcePayment.Allows(zero, [7], null, []));
        Assert.Equal("G", ResourcePayment.GeneratedBy(Conditional, []));
        var positive = new CostOption(0, "1", Sources: [Conditional, new(8, "Y")]);
        ResourceAllocation[] excess = [new(8, 0, "Y")];
        Assert.True(ResourcePayment.Allows(positive, [7, 8], null, excess));
        Assert.Equal("G", ResourcePayment.GeneratedBy(Conditional, excess));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PaymentForEitherQualifyingComponentActivatesBonus(int component)
    {
        var option = new CostOption(0, "2", Sources: [Conditional, new(8, "Y")],
            Components: [new("1"), new("1")]);
        ResourceAllocation[] assigned = [new(7, component, "G"), new(8, 1 - component, "Y")];
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, assigned));
        Assert.Equal("GG", ResourcePayment.GeneratedBy(Conditional, assigned));
    }

    [Fact]
    public void BonusCannotPayANonqualifyingComponentWithoutQualifyingPayment()
    {
        ResourceSource source = new(7, "R") { ConditionalBonus = new("Y", 1) };
        var option = new CostOption(0, "2", Sources: [source, new(8, "B")],
            Components: [new("1", ["B"]), new("1", ["Y"])]);
        ResourceAllocation[] invalid = [new(8, 0, "B"), new(7, 1, "Y")];
        Assert.False(ResourcePayment.Allows(option, [7, 8], null, invalid));
        Assert.Null(ResourcePayment.Allocate(option, [7, 8]));
        Assert.Equal("R", ResourcePayment.GeneratedBy(source, invalid));
    }

    [Fact]
    public void AllocationSearchUsesTheBonusOnlyWhenItsOwnSourceQualifies()
    {
        var option = new CostOption(0, "2", Sources: [Conditional, new(8, "YY")]);
        ResourceAllocation[] both = [new(7, 0, "GG")];
        ResourceAllocation[] other = [new(8, 0, "YY")];
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, both));
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, other));
        Assert.Equal("GG", ResourcePayment.GeneratedBy(Conditional, both));
        Assert.Equal("G", ResourcePayment.GeneratedBy(Conditional, other));
        var suggested = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(option, [7, 8]));
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, suggested));
    }
    [Fact]
    public void ImplicitGenerationIsAllowedOnlyWhenAllLegalAllocationsAgree()
    {
        var certain = new CostOption(0, "2", Sources: [Conditional]);
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.AllocateUnambiguousGeneration(certain, [7]));
        Assert.Equal("GG", ResourcePayment.GeneratedBy(Conditional, allocation));
        var ambiguous = certain with { Sources = [Conditional, new ResourceSource(8, "YY")] };
        Assert.Null(ResourcePayment.AllocateUnambiguousGeneration(ambiguous, [7, 8]));
        var zero = certain with { Cost = "0" };
        var excess = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.AllocateUnambiguousGeneration(zero, [7]));
        Assert.Equal("G", ResourcePayment.GeneratedBy(Conditional, excess));
    }

    [Fact]
    public void SeparateSourcesKeepTheirOwnQualifyingComponents()
    {
        var first = new ResourceSource(7, "R") { ConditionalBonus = new("R", 1) };
        var second = new ResourceSource(8, "Y") { ConditionalBonus = new("Y", 2) };
        var option = new CostOption(0, "2", Sources: [first, second],
            Components: [new("1"), new("1")]);
        ResourceAllocation[] swapped = [new(7, 1, "R"), new(8, 0, "Y")];
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, swapped));
        Assert.Equal("R", ResourcePayment.GeneratedBy(first, swapped));
        Assert.Equal("Y", ResourcePayment.GeneratedBy(second, swapped));
        ResourceAllocation[] matching = [new(7, 0, "R"), new(8, 1, "Y")];
        Assert.True(ResourcePayment.Allows(option, [7, 8], null, matching));
        Assert.Equal("RR", ResourcePayment.GeneratedBy(first, matching));
        Assert.Equal("YY", ResourcePayment.GeneratedBy(second, matching));
    }

    [Fact]
    public void RepeatedEquivalentIconsDoNotMultiplyAmbiguitySearch()
    {
        var source = new ResourceSource(7, new string('G', 18))
        { ConditionalBonus = new("G", 1) };
        var option = new CostOption(0, "18", Sources: [source]);
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.AllocateUnambiguousGeneration(option, [7]));
        Assert.True(ResourcePayment.Allows(option, [7], null, allocation));
        Assert.Equal(19, ResourcePayment.GeneratedBy(source, allocation).Length);
    }
}
