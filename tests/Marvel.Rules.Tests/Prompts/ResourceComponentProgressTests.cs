using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class ResourceComponentProgressTests
{
    [Theory]
    [InlineData("", 1, 2)]
    [InlineData("YY", 1, 0)]
    [InlineData("BB", 0, 2)]
    [InlineData("G", 1, 1)]
    [InlineData("YB", 0, 1)]
    [InlineData("GGB", 0, 0)]
    public void SharedIconsCoverAtMostOneComponentAndPreferTypedSlots(string icons, int generic, int energy)
    {
        var cost = Price(icons);
        var progress = ResourcePaymentProgress.Components(cost, [7], new Dictionary<string, long> { ["X"] = 2 })!;
        Assert.Equal([generic, energy], progress.Select(part => part.Remaining));
        Assert.Equal(new string('Y', energy), progress[1].RemainingResources);
        Assert.Equal([1, 2], progress.Select(part => part.Required));
        Assert.Equal(generic + energy == 0, ResourcePayment.Allocate(cost, [7], new Dictionary<string, long> { ["X"] = 2 }) is not null);
    }

    [Fact]
    public void UnresolvedAndInvalidDraftsDoNotAcquireComponentProgress()
    {
        Assert.Null(ResourcePaymentProgress.Components(Price("YY"), [7]));
        Assert.Null(ResourcePaymentProgress.Components(Price("YY"), [7, 7]));
        Assert.Null(ResourcePaymentProgress.Components(Price("YY"), [8]));
        Assert.Null(ResourcePaymentProgress.Components(Price("YY") with { Sources = [new(7, "Y"), new(7, "B")] }, [7]));
        Assert.Null(ResourcePaymentProgress.Components(new(0, "1", OrCost: "2"), []));
    }

    [Theory]
    [InlineData("GG", true, false, 2)]
    [InlineData("GG", false, false, 0)]
    [InlineData("YY", false, true, 1)]
    [InlineData("GG", false, true, 0)]
    public void PrintedAndDistinctRequirementsUseTheSameAllocationRules(string icons, bool printed, bool distinct, int remaining)
    {
        var cost = new CostOption(0, "2", Sources: [new(7, icons)], Components:
            [new ResourceCost("2", printed ? ["YY"] : null, printed) { DistinctTypes = distinct }]);
        var progress = Assert.Single(ResourcePaymentProgress.Components(cost, [7])!);
        Assert.Equal(remaining, progress.Remaining);
        Assert.Equal(printed, progress.Printed);
        Assert.Equal(distinct, progress.DistinctTypes);
    }

    private static CostOption Price(string icons) => new(0, "1+X", Sources: [new(7, icons)],
        Variables: [new("X", 0, 5)], Components: [new("1"), new ResourceCost("X") { RepeatedResource = 'Y' }]);
}
