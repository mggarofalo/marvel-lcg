using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored rows on a nonpublic board; Thor product admission remains closed.
public sealed class ThorLightningStrikeCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01088", true)]
    [InlineData("01089", false)]
    public void ActualLightningRequiresEnergyBeyondItsPrintedPrice(string resource, bool offered)
    {
        // Printed 06006 has cost 1 and separately requires X energy resources.
        var world = ThorActualFixture.Board("06001a");
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        world.CreateCard(resource, world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        Assert.Equal(offered, AuthoredCards.Runner().Actions(world, 0).Any(a => a.Card == source.ObjectId));
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ActualLightningDoesNotExposeUltronWhileDefeatingHisLastDrone()
    {
        // Vendored ruling 3eb442853617fe14: drones remain in play when
        // Ultron III's eligibility for the simultaneous damage is determined.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.VillainDeck));
        var ultron = world.CreateCard("01136", world.AreaOf(DeckType.VillainArea));
        var drone = world.CreateCard("01004", world.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(0), cardOwner: 0));
        drone.AssignProfile(AuthoredCards.DroneProfile);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var printed = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var cost = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("1+X", cost.Cost);
        Assert.Equal(2, cost.ResourceCosts.Count);
        var values = new Dictionary<string, long> { ["X"] = 2 };
        var allocation = ResourcePayment.Allocate(cost, [energy.ObjectId, printed.ObjectId], values);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, printed.ObjectId], [], values, allocation));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, drone.Area.Type);
        Assert.Equal(0, ultron.Damage);
        Assert.Empty(world.Agenda.Outstanding);
        // The otherwise identical next printed event can damage the exposed villain.
        var next = world.CreateCard("06006", world.Seats[0].Hand);
        var nextEnergy = world.CreateCard("01088", world.Seats[0].Hand);
        var nextPrinted = world.CreateCard("01089", world.Seats[0].Hand);
        var nextAction = Assert.Single(runner.Actions(world, 0), a => a.Card == next.ObjectId);
        var nextCost = Assert.Single(runner.Describe(world, nextAction).CostOptions);
        var nextAllocation = ResourcePayment.Allocate(nextCost, [nextEnergy.ObjectId, nextPrinted.ObjectId], values);
        Assert.NotNull(nextAllocation);
        runner.ExecuteAction(world, new PlayerAction(nextAction, [nextEnergy.ObjectId, nextPrinted.ObjectId], [], values, nextAllocation));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, ultron.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ActualLightningFromSecondSeatDefeatsBothOwnMinionsAndHitsOnlyItsVillain()
    {
        // Printed 06006: "the villain and each minion engaged with you".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("06006", world.Seats[1].Hand);
        var energy = world.CreateCard("01088", world.Seats[1].Hand);
        var printed = world.CreateCard("01089", world.Seats[1].Hand);
        var own = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var second = world.CreateCard("01101", own.Area);
        own.TakeDamage(1);
        second.TakeDamage(1);
        var other = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        var cost = Assert.Single(runner.Describe(world, action).CostOptions);
        var values = new Dictionary<string, long> { ["X"] = 2 };
        var allocation = ResourcePayment.Allocate(cost, [energy.ObjectId, printed.ObjectId], values);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, printed.ObjectId], [], values, allocation));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, own.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Equal(0, other.Damage);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData("06001a", true)]
    [InlineData("06001b", false)]
    public void PrintedHeroActionRequiresHeroFormWithOtherwisePayableControl(string form, bool offered)
    {
        var world = ThorActualFixture.Board(form);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        Assert.Equal(offered, AuthoredCards.Runner().Actions(world, 0).Any(a => a.Card == source.ObjectId));
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualLightningPaysPrintedAndChosenCostsAndRetainsIgnoredTough(bool aerial)
    {
        // Printed 06006: "Spend X [energy] resources" and "the villain and each minion
        // engaged with you". Vendored erratum: Lightning Strike "is not an Attack event".
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var jarnbjorn = world.CreateCard("06019", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01089", world.Seats[0].Hand);
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
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.UpgradesArea, jarnbjorn.Area.Type);
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
