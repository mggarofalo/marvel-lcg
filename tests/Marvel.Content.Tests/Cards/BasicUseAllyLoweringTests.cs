using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class BasicUseAllyLoweringTests
{
    private const string Cost = """{"exhaustChosen":{"from":{"query":"alliesYouControl"},"count":1,"matchingBasicPower":"true"}}""";
    private const string Boost = """{"boostBasicUse":"exhaustedAllyPower"}""";

    private static AbilityBook Book(string effect, string? cost = Cost, string timing = "Interrupt") =>
        AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"06032","abilities":[{
          "trigger":{"event":"WhenHeroBasicUseInitiated","timing":"{{{timing}}}","actor":"you","form":"hero","player":"you"},
          {{{(cost is null ? "" : $"\"cost\":{cost},")}}}"effect":{{{effect}}}
        }]}]}
        """);

    [Theory]
    [InlineData("seq")]
    [InlineData("attack")]
    [InlineData("choose")]
    public void NestedMatchingBoostCannotEscapeItsRequiredReceipt(string wrapper)
    {
        string effect = wrapper switch
        {
            "seq" => $$$"""{"seq":[{{{Boost}}}]}""",
            "attack" => $$$"""{"attack":{"target":{"query":"villain"},"effect":{{{Boost}}}}}""",
            _ => $$$"""{"choose":{"options":[{{{Boost}}},{"draw":{"player":"you","count":1}}]}}""",
        };
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Book(effect, cost: null)));
        Assert.Contains("selected ally cost", error.Message);
    }

    [Theory]
    [InlineData("power")]
    [InlineData("twice")]
    [InlineData("choice")]
    public void ReceiptCannotProjectIntoAnotherPowerOrMultipleOrOptionalApplications(string shape)
    {
        string effect = shape switch
        {
            "power" => $$$"""{"attack":{"target":{"query":"villain"},"effect":{{{Boost}}}}}""",
            "twice" => $$$"""{"seq":[{{{Boost}}},{{{Boost}}}]}""",
            _ => $$$"""{"choose":{"options":[{{{Boost}}},{"draw":{"player":"you","count":1}}]}}""",
        };
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Book(effect)));
        Assert.Contains("one sequential application", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompoundMatchingCostsAreRejectedAtEveryDepth(bool nested)
    {
        string cost = $$$"""{"seq":[{{{Cost}}},{"exhaust":"you"}]}""";
        if (nested) cost = $$$"""{"seq":[{{{cost}}}]}""";
        Assert.Throws<RulesNotImplementedException>(() => AbilityLowering.Book(Book(Boost, cost)));
    }

    [Fact]
    public void MatchingUseMustBeAnInterrupt()
    {
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Book(Boost, timing: "Response")));
        Assert.Contains("hero basic-use interrupt", error.Message);
    }
}
