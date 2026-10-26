using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowQuincarrierTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resource-ability.1")]
    public void QuincarrierHasUnrestrictedResourceInBothForms(bool hero)
    {
        // A resource ability can trigger "anytime" its controller is "generating resources to pay a cost".
        var world = WidowEncounterFixture.Board(hero);
        var runner = WidowEncounterFixture.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("08023", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var target = world.CreateCard("01026", world.Seats[1].Hand);
        var price = CardPayment.Price(world, world.Facts, world.Seats[1], target)!;
        Assert.Contains(price.Sources!, s => s.Effect == source.ObjectId && s.Generates == "G");
        var paid = AbilityResourcePayment.Spend(world, runner, new(target, 1, [source.ObjectId], 1, "R")
            { Allocations = [new(source.ObjectId, 0, "R")] }, []);
        Assert.True(paid.IsComplete);
        Assert.Equal("R", paid.Paid);
        Assert.False(source.Ready);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayRestrictionRequiresCurrentIdentityAvengerEvenWithAvengerAlly(bool hero)
    {
        var world = WidowEncounterFixture.Board(hero);
        world.CreateCard("08012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var source = world.CreateCard("08023", world.Seats[1].Hand);
        world.CreateCard("08020", world.Seats[1].Hand);
        world.CreateCard("08021", world.Seats[1].Hand);
        var control = world.CreateCard("01092", world.Seats[1].Hand);
        Assert.NotNull(CardPayment.Price(world, world.Facts, world.Seats[1], control));
        Assert.Equal(hero, CardPayment.Price(world, world.Facts, world.Seats[1], source) is not null);
    }
}
