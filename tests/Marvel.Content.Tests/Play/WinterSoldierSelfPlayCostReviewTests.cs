using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Independent synthetic contract probes; Black Widow remains outside public admission.
public sealed class WinterSoldierSelfPlayCostReviewTests
{
    [Fact]
    [Rule("rr:traits.1")]
    public void ScalarCountsCardsOnceAndRepricesAfterTraitLossAndRestoration()
    {
        // "Some card abilities reference cards that possess or lack specific traits."
        // Printed 08002 counts Preparation cards; scalar two tests the chosen DSL contract.
        var (_, world) = Playing(_ => { }, abilities: Runner(2));
        var candidate = world.CreateCard("08002", world.Seats[0].Hand);
        var preparation = world.CreateCard("08017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        var otherAlly = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Assert.Equal(2, Cost());
        using var duplicateGrant = world.Effects.Register(new(EffectSource.LastingEffect,
            Traits.Granted + "PREPARATION", Affects: preparation.ObjectId));
        Assert.Equal(2, Cost());
        using (world.Effects.Register(new(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "PREPARATION", Affects: preparation.ObjectId)))
            Assert.Equal(4, Cost());
        Assert.Equal(2, Cost());
        using (world.Effects.Register(new(EffectSource.LastingEffect,
            Traits.Granted + "PREPARATION", Affects: otherAlly.ObjectId)))
            Assert.Equal(0, Cost());
        Assert.Equal(2, Cost());

        long Cost() => CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount;
    }

    [Fact]
    [Rule("rr:ownership-and-control.2")]
    [Rule("rr:ownership-and-control.8")]
    public void PlayerOwnedEncounterCardWithPreparationDoesNotReduceCost()
    {
        // "Encounter cards are considered to be under the control of the scenario."
        // "You control" refers to cards in play currently under that player's control.
        // Synthetic player ownership and a granted trait isolate control from ownership.
        var runner = Runner(1);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var candidate = world.CreateCard("08002", world.Seats[0].Hand);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new(EffectSource.LastingEffect,
            Traits.Granted + "PREPARATION", Affects: minion.ObjectId));
        Assert.Equal(0, minion.Owner);
        Assert.Equal(World.Scenario, CardControl.ControllerOf(world, minion));
        Assert.True(Traits.Has(world, minion, "PREPARATION", world.Facts));
        Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount);
        world.CreateCard("08017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], candidate).Amount);
        Assert.Empty(runner.Constant(world, candidate));
        World.MoveToTop(candidate, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Assert.Empty(runner.Constant(world, candidate));
        Assert.Equal(0, runner.SelfPlayCostReduction(world, candidate, 0));
    }

    [Theory]
    [InlineData("{\"and\":[DISCOUNT,{\"ready\":\"this\"}]}")]
    [InlineData("{\"if\":{\"test\":{\"titleInPlay\":\"Aunt May\"},\"then\":DISCOUNT}}")]
    [InlineData("{\"if\":{\"test\":{\"titleInPlay\":\"Aunt May\"},\"then\":{\"ready\":\"this\"},\"else\":DISCOUNT}}")]
    public void NestedPricingInEitherBranchFailsClosedBeforeGameplay(string wrapper)
    {
        // The engine chooses to admit only direct unconditional candidate pricing.
        const string discount = """
            {"reduceSelfCostPerControlledTrait":{"trait":"PREPARATION","amount":1}}
            """;
        string effect = wrapper.Replace("DISCOUNT", discount, StringComparison.Ordinal);
        string book = """
            {"cards":[{"card":"08002","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},"effect":EFFECT
            }]}]}
            """.Replace("EFFECT", effect, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
        Assert.Contains("direct unconditional Constant", error.Message);
    }

    private static AbilityRunner Runner(int amount) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"08002","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},
          "effect":{"reduceSelfCostPerControlledTrait":{"trait":"PREPARATION","amount":AMOUNT}}
        }]}]}
        """.Replace("AMOUNT", amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal)));
}
