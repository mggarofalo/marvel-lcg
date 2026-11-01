using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// Malformed shapes of the vocabulary the Quicksilver pack added are refused before gameplay.
public sealed class QuicksilverLoweringTests
{
    private const string Trigger =
        """{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"}""";

    [Theory]
    [InlineData("""{"chooseHealRecipients":{"from":{"query":"heroesAndAllies"},"amount":1}}""", "maximum")]
    [InlineData("""{"chooseHealRecipients":{"from":{"query":"heroesAndAllies"},"amount":0,"maximum":2}}""", "amount")]
    [InlineData("""{"chooseStatusRecipients":{"from":{"query":"enemies"},"status":"stunned","maximum":2,"count":2}}""", "either")]
    [InlineData("""{"boostBasicUse":{"result":"energy"}}""", "boostIconsDiscarded")]
    [InlineData("""{"seqEachTargeted":[{"draw":{"player":"you","count":1}}]}""", "choice option")]
    public void NewEffectsRefuseAnyOtherShape(string effect, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Book(Ability(effect))));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APrintedXCostAcceptsOnlyThePrintedSpelling()
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Book(
            Ability("""{"draw":{"player":"you","count":1}}""", """ "cost":{"spendX":"energy"}, """))));
        Assert.Contains("printed", failure.Message, StringComparison.Ordinal);

        var program = AbilityLowering.Book(Book(
            Ability("""{"draw":{"player":"you","count":1}}""", """ "cost":{"spendX":"printed"}, """)));
        var cost = Assert.IsType<AbilityCost.SpendX>(Assert.Single(program.On("14006")).Cost);
        Assert.Null(cost.Resource);
    }

    [Theory]
    [InlineData("""  "limitGroup":"", "limitPerPhase":1, """, """  "limitGroup":"", "limitPerPhase":1, """, "nonempty")]
    [InlineData("""  "limitGroup":"speed", "limitPerPhase":1, """, "", "several abilities")]
    [InlineData("""  "limitGroup":"speed", "limitPerPhase":1, """, """  "limitGroup":"speed", "limitPerRound":1, """, "shared limit")]
    public void ALimitGroupNeedsOneSharedLimitOnSeveralAbilities(string first, string second, string says)
    {
        string draw = """{"draw":{"player":"you","count":1}}""";
        string abilities = second.Length == 0
            ? Ability(draw, first)
            : Ability(draw, first) + "," + Ability(draw, second);
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Book(abilities)));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    private static string Ability(string effect, string extra = "") =>
        $$"""{"trigger":{{Trigger}},{{extra}}"effect":{{effect}}}""";

    private static AbilityBook Book(string abilities) =>
        AbilityCatalog.Parse($$"""{"cards":[{"card":"14006","abilities":[{{abilities}}]}]}""");
}
