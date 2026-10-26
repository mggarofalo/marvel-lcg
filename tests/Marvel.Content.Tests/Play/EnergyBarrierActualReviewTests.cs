using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;

using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EnergyBarrierActualReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01001a")]
    [InlineData("01001b")]
    public void NonAttackDamageToSecondSeatsIdentityOffersBarrierInEitherForm(string form)
    {
        // 05017: "When you would take any amount of damage", in either form.
        var world = Hero("05001a", 2);
        var identity = world.Seats[1].IdentityCard;
        identity = world.CreateCard(form, world.Seats[1].Hero);
        world.Seats[1].IdentityCard = identity;
        Statuses.Give(world, identity, Statuses.Stunned);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(0), cardOwner: 0));
        var barrier = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), cardOwner: 1));
        barrier.PlaceTokens("c_reflection", 1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        var replacement = DamagePlacement.Replace(world,
            new(villain, identity, 2, "Review", "Damage"), events);
        var offered = Assert.Single(runner.Waiting(world,
            replacement.Execution.Occurrence, WindowKind.Interrupt));
        Assert.Equal(barrier.ObjectId, offered.Card);
        Assert.Equal(1, offered.Player);
        Assert.Equal(1, barrier.Tokens["c_reflection"]);
        Assert.Equal(2, replacement.ResolvedAmount);
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OtherIdentityAndOwnAllyCannotSpendTheControllersBarrier(bool ownAlly)
    {
        // 05017 says "you", not a character that you control or any player.
        var world = Hero("05001a", 2);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var target = ownAlly
            ? world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1))
            : world.Seats[0].IdentityCard;
        var barrier = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), cardOwner: 1));
        barrier.PlaceTokens("c_reflection", 3);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, Cards, new(villain, target, 1, "Review", "Damage"), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(1, target.Damage);
        Assert.Equal(3, barrier.Tokens["c_reflection"]);
        Assert.Equal(0, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}




