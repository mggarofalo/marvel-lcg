using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreDamagePreventionTests : CoreCardTestBase
{
    [Rule("rr:prevent")]
    [Rule("rr:prevent.1")]
    [Rule("rr:prevent.1.1")]
    [Fact]
    public void CosmicFlightPreventsExactlyThreeDamage()
    {
        // Preventing damage "reduce[s] the amount of damage the target takes";
        // the amount "dealt" is not reduced. Five is still dealt, while only
        // two is taken after Cosmic Flight prevents three.
        var world = Hero("01010a");
        var flight = world.CreateCard(
            "01017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01087", world.Seats[0].Deck);
        var villain = world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = Occurrence.ForAttack(
            1, [Steps.DamageWouldBeDealt], world, Cards, villain.ObjectId,
            world.Seats[0].IdentityCard.ObjectId, player: 0);
        var interrupt = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == flight.ObjectId);

        runner.ResolveWindow(world, occurrence, interrupt, new Decision(interrupt.Card, [], []));

        DamageReplacement replacement = DamagePlacement.Replace(world,
            new DamageRequest(villain, world.Seats[0].IdentityCard, 5,
                Steps.DamageWouldBeDealt, "Damage"), []);
        Assert.True(replacement.IsComplete);
        Assert.Equal(5, replacement.ResolvedAmount);
        Assert.Equal(2, runner.WouldTake(
            world, world.Seats[0].IdentityCard, villain, 5, []));
        Assert.Equal(DeckType.DiscardPile, flight.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:damage.step.3")]
    [Fact]
    public void ToughExpiresPreventionCommittedToThatDamageInstance()
    {
        // Cosmic Flight has already committed its one-use prevention when
        // Tough stops the whole instance at step 2. Both defenses expire with
        // that damage; the prevention cannot leak into a later instance.
        var world = Hero("01010a");
        var flight = world.CreateCard(
            "01017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        var hero = world.Seats[0].IdentityCard;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Statuses.Give(world, hero, Statuses.Tough);
        var occurrence = Occurrence.ForAttack(
            1, [Steps.DamageWouldBeDealt], world, Cards,
            villain.ObjectId, hero.ObjectId, player: 0);
        var interrupt = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == flight.ObjectId);
        runner.ResolveWindow(world, occurrence, interrupt, new Decision(interrupt.Card, [], []));

        DamagePlacement.Resolve(world, Cards,
            new(villain, hero, 5, "test", "Damage"), []);
        DamagePlacement.Resolve(world, Cards,
            new(villain, hero, 2, "test", "Damage"), []);

        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(2, hero.Damage);
        Assert.DoesNotContain(
            world.Effects.Active(), effect => effect.Kind == "preventDamage");
    }

}
