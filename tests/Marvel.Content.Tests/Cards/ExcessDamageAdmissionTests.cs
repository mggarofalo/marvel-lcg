using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class ExcessDamageAdmissionTests
{
    [Theory]
    [InlineData("Action", "", "grant", "")]
    [InlineData("Constant", "", "grantUntil", ",\"until\":\"EndOfRound\"")]
    [InlineData("Constant", ",\"when\":{\"inForm\":{\"player\":\"you\",\"form\":\"hero\"}}", "grant", "")]
    [InlineData("Constant", "", "grant", ",\"amount\":0")]
    [InlineData("Constant", "", "grant", ",\"amount\":2")]
    [InlineData("Constant", "", "grant", ",\"attackScope\":\"basic\"")]
    public void UnsupportedRoutingFormsFailBeforeGameplay(string timing, string extra, string operation, string fields)
    {
        var exception = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"07022","abilities":[{
              "trigger":{"timing":"{{{{timing}}}}"}{{{{extra}}}},
              "effect":{"{{{{operation}}}}":{"card":"attachedTo","keyword":"excessDamageAsThreat"{{{{fields}}}}}}
            }]}]}
            """)));
        Assert.Contains("07022", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedRoutingIsRejectedEvenInAnUnreachedBranch()
    {
        var exception = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"07022","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"grant":{"card":"attachedTo","keyword":"excessDamageAsThreat"}}}}
            }]}]}
            """)));
        Assert.Contains("direct unconditional Constant", exception.Message, StringComparison.Ordinal);
    }
}
