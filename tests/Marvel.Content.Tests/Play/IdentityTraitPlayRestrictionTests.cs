using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class IdentityTraitPlayRestrictionTests
{
    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, false)]
    [Rule("rr:identity.4")]
    [Rule("rr:traits.1")]
    public void HonoraryAvengerRequiresTheActingIdentitysCurrentTrait(bool hero, bool granted, bool lost, bool permitted)
    {
        // Honorary Avenger: "Play only if your identity has the Avenger trait."
        // rr:identity.4: "The faceup side of the identity card is considered to be in play."
        // rr:traits.1: abilities "reference cards that possess or lack specific traits."
        var runner = AuthoredCards.Runner();
        Card? upgrade = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            if (hero) board.Seats[0].IdentityCard.TurnTo("01010a");
            if (granted) board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "trait:AVENGER", 1, Affects: board.Seats[0].IdentityCard.ObjectId));
            if (lost) board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Characteristics.Lost + Traits.Granted + "AVENGER", 1, Affects: board.Seats[0].IdentityCard.ObjectId));
            upgrade = board.CreateCard("03025", board.Seats[0].Hand);
            second = board.CreateCard("03025", board.Seats[0].Hand);
        }, heroes: ["captain_marvel", "spider_man"], abilities: runner);
        var offers = game.Pending!.Affordances.Where(offer => offer.AnchorId == upgrade!.ObjectId && offer.PlaysCard).ToArray();
        Assert.Equal(permitted ? 1 : 0, offers.Length);
        if (!permitted) return;
        var target = world.Seats[1].IdentityCard;
        long health = DamagePlacement.Health(world, world.Facts, target);
        Assert.Contains(target.ObjectId, offers[0].Targets!.Legal);
        game.Resolve(new Decision(offers[0].Id, [target.ObjectId]));
        Assert.Equal(target.ObjectId, upgrade!.Area.Host);
        Assert.True(Traits.Has(world, target, "AVENGER", world.Facts));
        Assert.Equal(health + 1, DamagePlacement.Health(world, world.Facts, target));
        var next = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == second!.ObjectId && offer.PlaysCard);
        Assert.DoesNotContain(target.ObjectId, next.Targets!.Legal);
        Assert.DoesNotContain(world.TheCardIn(DeckType.VillainArea)!.ObjectId, next.Targets.Legal);
        Discard.Card(world, upgrade, "test", []);
        Assert.False(Traits.Has(world, target, "AVENGER", world.Facts));
        Assert.Equal(health, DamagePlacement.Health(world, world.Facts, target));
    }
}
