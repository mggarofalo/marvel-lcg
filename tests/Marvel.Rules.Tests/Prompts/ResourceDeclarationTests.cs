using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class ResourceDeclarationTests
{
    [Rule("rr:printed.1.1")]
    [Rule("rr:resource.2")]
    [Theory]
    [InlineData(true, 'G', "G")]
    [InlineData(false, 'G', "BGRY")]
    [InlineData(true, 'R', "R")]
    [InlineData(false, 'R', "R")]
    [InlineData(false, '?', "")]
    public void OfferedDeclarationsAgreeWithPaymentValidation(bool printed, char icon, string expected)
    {
        // "Wild resources ... cannot be spent as other resource types for such a cost."
        // Otherwise, "Wild resources can be used as their type or any of the other types."
        var component = new ResourceCost("1", Printed: printed);
        Assert.Equal(expected, new string(component.Declarations(icon).Order().ToArray()));
        var cost = new CostOption(0, "1", Sources: [new ResourceSource(7, icon.ToString())],
            Components: [component]);
        foreach (char declaration in "BGRY?")
            Assert.Equal(expected.Contains(declaration), ResourcePayment.Allows(cost, [7], null,
                [new ResourceAllocation(7, 0, declaration.ToString())]));
    }
}
