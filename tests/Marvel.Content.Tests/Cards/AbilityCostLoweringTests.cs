using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class AbilityCostLoweringTests
{
    private static readonly AbilityLocation Location = new("01005", 0, "cost");

    [Fact]
    public void EveryCoreArrowCostLowersWithoutAWorld()
    {
        var costs = AuthoredCards.Book.Abilities.Where(ability => ability.Cost is not null).ToList();
        Assert.NotEmpty(costs);
        foreach (var ability in costs)
        {
            var cost = ability.Cost!;
            Assert.IsAssignableFrom<AbilityCost>(AbilityLowering.Cost(
                Node(cost), new AbilityLocation(ability.Card, 0, "cost")));
        }
    }

    [Fact]
    public void CombinedCostRetainsOrderAndDistinctDamagePaymentSemantics()
    {
        var cost = Assert.IsType<AbilityCost.Sequence>(Lower(
            """{"seq":[{"exhaust":"this"},{"dealDamage":{"cards":"you","amount":2}},{"takeDamage":{"cards":"you","amount":1}}]}"""));

        Assert.Collection(cost.Costs,
            first => Assert.Equal(new AbilityCost.Exhaust(AbilityCostCard.Source), first),
            second => Assert.Equal(new AbilityCost.Damage(AbilityCostCard.Identity, 2, false), second),
            third => Assert.Equal(new AbilityCost.Damage(AbilityCostCard.Identity, 1, true), third));
    }

    [Theory]
    [InlineData("spend", false)]
    [InlineData("spendPrinted", true)]
    public void PrintedAndGeneratedResourceCostsStayDistinct(string kind, bool printed)
    {
        Assert.Equal(new AbilityCost.Spend("BYR", printed), Lower($$"""{"{{kind}}":"BYR"}"""));
    }

    [Fact]
    public void CostSelectionHasOneCheckedRange()
    {
        Assert.Equal(new AbilityCost.ExhaustChosen(AbilityCardQuery.HeroesAndAllies,
            new AbilityCostRange.UpTo(2)), Lower(
            """{"exhaustChosen":{"from":{"query":"heroesAndAllies"},"upTo":2}}"""));
        Assert.Equal(new AbilityCost.DiscardFromHand(new AbilityCostRange.Any()),
            Lower("""{"discardAnyFromHand":"yourHand"}"""));
    }

    [Theory]
    [InlineData("{\"exhaustChosen\":{\"from\":{\"query\":\"heroesAndAllies\"},\"count\":1,\"upTo\":2}}", "several selection ranges")]
    [InlineData("{\"exhaustChosen\":{\"from\":{\"query\":\"villain\"}}}", "supported exhaust-cost relation")]
    [InlineData("{\"exhaustChosen\":{\"from\":{\"query\":\"heroesAndAllies\"},\"anyNumber\":\"false\"}}", "expected 'true'")]
    [InlineData("{\"discardFromHand\":0}", "positive integer")]
    [InlineData("{\"discardFromHand\":2147483648}", "exceeds the engine range")]
    [InlineData("{\"removeCounters\":{\"card\":\"this\",\"counter\":\"\",\"count\":1}}", "nonempty counter")]
    [InlineData("{\"removeCounters\":{\"card\":\"this\",\"counter\":\"web\",\"count\":0}}", "positive integer")]
    [InlineData("{\"takeDamage\":{\"cards\":\"chosen\",\"amount\":1}}", "not a card supported by this cost")]
    [InlineData("{\"takeDamage\":{\"cards\":\"you\",\"amount\":1,\"ignorePrevention\":1}}", "unknown argument")]
    [InlineData("{\"spend\":\"Q\"}", "supported resource symbols")]
    [InlineData("{\"spendEnergyX\":\"B\"}", "expected 'Y'")]
    [InlineData("{\"draw\":{\"player\":\"you\",\"count\":1}}", "not a cost operation")]
    public void InvalidCostsAreRejectedBeforePayment(string json, string expected)
    {
        var failure = Assert.Throws<AbilityException>(() => Lower(json));

        Assert.Contains("'01005' ability 0 at 'cost/", failure.Message, StringComparison.Ordinal);
        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACombinedCostCannotHideAnInvalidLaterComponent()
    {
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"seq":[{"exhaust":"this"},{"discardFromHand":"one"}]}"""));

        Assert.Contains("cost/seq/1/discardFromHand", failure.Message, StringComparison.Ordinal);
    }

    private static AbilityCost Lower(string json)
    {
        var cost = AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"01005","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{{{json}}},"effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """).Abilities[0].Cost!;
        return AbilityLowering.Cost(Node(cost), Location);
    }

    private static AbilityValue.Map Node(AbilityNode node) =>
        new(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [node.Kind] = node.Argument });
}
