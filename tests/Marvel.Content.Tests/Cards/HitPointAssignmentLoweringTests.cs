using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class HitPointAssignmentLoweringTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("{\"damageOn\":\"you\"}")]
    public void AssignmentRequiresAPositiveConstant(string amount)
    {
        var json = """
            {"cards":[{"card":"test","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
            "effect":{"setHitPoints":{"card":"you","amount":AMOUNT}}}]}]}
            """.Replace("AMOUNT", amount, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("amount", error.Message);
    }
}
