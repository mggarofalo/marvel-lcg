using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class FirstAllyCostLoweringTests
{
    [Theory]
    [InlineData("Action")]
    [InlineData("Response")]
    public void HistoryDependentDiscountMustBeConstant(string timing)
    {
        var json = """
            {"cards":[{"card":"test","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"TIMING","subject":"this"},
            "effect":{"reduceFirstAllyCost":1}}]}]}
            """.Replace("TIMING", timing, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("direct unconditional Constant", error.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void ReductionMustBePositive(string amount)
    {
        var json = """
            {"cards":[{"card":"test","abilities":[{
            "trigger":{"timing":"Constant","subject":"this"},
            "effect":{"reduceFirstAllyCost":AMOUNT}}]}]}
            """.Replace("AMOUNT", amount, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("positive integer", error.Message);
    }
}
