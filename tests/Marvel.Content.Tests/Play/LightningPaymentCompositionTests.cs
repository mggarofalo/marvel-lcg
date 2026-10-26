using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic selection and composition contracts; no Lightning Strike row is admitted.</summary>
public sealed class LightningPaymentCompositionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntheticLightningPaysPrintedAndChosenCostsAndRetainsIgnoredTough(bool aerial)
    {
        // Printed 06006: "Spend X [energy] resources" and "the villain and each minion
        // engaged with you". Vendored erratum: Lightning Strike "is not an Attack event".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "when":{"inForm":{"player":"you","form":"hero"}},
              "cost":{"spendEnergyX":"Y"},
              "effect":{"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},
                "then":{"dealDamage":{"cards":{"query":"villainAndMinionsEngagedWithYou"},"amount":{"result":"energy"},"ignoresTough":1}},
                "else":{"dealDamage":{"cards":{"query":"villainAndMinionsEngagedWithYou"},"amount":{"result":"energy"}}}}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var printed = world.CreateCard("01089", world.Seats[0].Hand);
        var own = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var other = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, own, Statuses.Tough);
        Statuses.Give(world, villain, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var cost = Assert.Single(runner.Describe(world, action).CostOptions);
        var values = new Dictionary<string, long> { ["X"] = 2 };
        var allocation = ResourcePayment.Allocate(cost, [energy.ObjectId, printed.ObjectId], values);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, printed.ObjectId], [], values, allocation));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(aerial ? 2 : 0, villain.Damage);
        Assert.Equal(aerial ? 2 : 0, own.Damage);
        Assert.Equal(aerial, Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(aerial, Statuses.Has(world, own, Statuses.Tough));
        Assert.Equal(0, other.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, printed.Area.Type);
    }

}
