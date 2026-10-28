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
}
