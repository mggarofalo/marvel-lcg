using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AllyControllerPowerTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:guard.1")]
    [Rule("rr:ownership-and-control.5")]
    [Rule("rr:ally.3")]
    public void BasicAttackUsesControllersGuardRestrictionAndRetainsAllyConsequence(bool transferred)
    {
        // Guard: "the engaged player cannot attack any villain". Changed control moves the card to that player's area.
        var world = Hero("01001a", 2);
        world.Abilities = AuthoredCards.Runner();
        int controller = transferred ? 1 : 0;
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (transferred) World.MoveToTop(ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(controller)));
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var guard = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(controller)));
        var events = new List<GameEvent>();

        Assert.Throws<RulesNotImplementedException>(() =>
            AllyBasicPowers.AllyPower(world, Cards, ally, villain, BasicPowers.AttackVerb, events));
        Assert.True(ally.Ready);
        Assert.Empty(events);
        AllyBasicPowers.AllyPower(world, Cards, ally, guard, BasicPowers.AttackVerb, events);
        Sequence.Finish(world, Cards, AuthoredCards.Runner(), events);

        Assert.Equal(0, ally.Owner);
        Assert.Equal(controller, CardControl.ControllerOf(world, ally));
        Assert.Equal(1, guard.Damage);
        Assert.Equal(1, ally.Damage);
        Assert.False(ally.Ready);
        Assert.Equal(0, villain.Damage);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.False(world.Agenda.IsBusy);
    }
}
