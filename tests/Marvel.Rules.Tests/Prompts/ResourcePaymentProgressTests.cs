using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class ResourcePaymentProgressTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("2", 0)]
    [InlineData("3", 1)]
    [InlineData("5", 3)]
    public void CountsEveryIconAndClampsExcess(string required, int remaining)
    {
        var cost = new CostOption(0, required, Sources: [new(7, "GG"), new(8, "Y")]);
        Assert.Equal(remaining, ResourcePaymentProgress.RemainingRequired(cost, [7]));
    }

    [Theory]
    [InlineData("typed")]
    [InlineData("repeated")]
    [InlineData("printed")]
    [InlineData("alternative")]
    [InlineData("variable")]
    [InlineData("components")]
    [InlineData("invalid")]
    public void ComplexCostsDoNotAcquireAGenericShortfall(string shape)
    {
        var cost = shape switch
        {
            "typed" => new CostOption(0, "2", Rule: ["Y"]),
            "repeated" => new CostOption(0, "2", Components: [new ResourceCost("2") { RepeatedResource = 'Y' }]),
            "printed" => new CostOption(0, "2", Components: [new ResourceCost("2", Printed: true)]),
            "alternative" => new CostOption(0, "2", OrCost: "1", OrRule: ["Y"]),
            "variable" => new CostOption(0, "X", Variables: [new VariableRequest("X", 0, 3)]),
            "components" => new CostOption(0, "2", Components: [new("1"), new("1")]),
            _ => new CostOption(0, "-1"),
        };
        Assert.Null(ResourcePaymentProgress.RemainingRequired(cost, []));
    }

    [Fact]
    public void UnknownDuplicateAndAmbiguousSourcesDoNotProduceProgress()
    {
        var cost = new CostOption(0, "3", Sources: [new(7, "G")]);
        Assert.Null(ResourcePaymentProgress.RemainingRequired(cost, [8]));
        Assert.Null(ResourcePaymentProgress.RemainingRequired(cost, [7, 7]));
        Assert.Null(ResourcePaymentProgress.RemainingRequired(cost with { Sources = [new(7, "G"), new(7, "Y")] }, [7]));
    }
}
