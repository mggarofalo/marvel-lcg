using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaSupportTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:form-change-form.5")]
    public void ShieldGrantsHeroDefenseAndRetaliateEvenWhenExhausted(bool exhausted)
    {
        // rr:form-change-form.5: "card abilities that interact with their hero do not interact with their identity."
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            var shield = board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            if (exhausted) shield.Exhaust();
        });
        var identity = world.Seats[0].IdentityCard;
        Assert.Equal(3, StateFields.Modified(world, identity, "defense", world.Facts, world.Players));
        Assert.Equal(1, StateFields.Modified(world, identity, "retaliate", world.Facts, world.Players));
        identity.TurnTo("03001b");
        Assert.Equal(0, StateFields.Modified(world, identity, "defense", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, identity, "retaliate", world.Facts, world.Players));
        identity.TurnTo("03001a");
        Assert.Equal(3, StateFields.Modified(world, identity, "defense", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resource-ability.1")]
    public void SerumPaysPhysicalInEitherFormAndCannotBeSpentTwice(bool hero)
    {
        // rr:resource-ability.1: "anytime the player who controls the ability is generating resources to pay a cost."
        Card? serum = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
            serum = board.CreateCard("03010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)),
            hero);
        var runner = AuthoredCards.Runner();
        var shield = world.CreateCard("03009", world.Seats[0].Hand);
        var generator = Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, shield),
            item => item.Effect == serum!.ObjectId);
        Assert.Equal("R", generator.Generates);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], shield, [serum!.ObjectId], []);
        Assert.False(serum.Ready);
        Assert.Equal(DeckType.UpgradesArea, serum.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, shield.Area.Type);
        var second = world.CreateCard("03007", world.Seats[0].Hand);
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, second),
            item => item.Effect == serum.ObjectId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ApartmentDrawsWhenUndamagedAndHealsOnlyOneDamage(int damage)
    {
        Card? apartment = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            apartment = board.CreateCard("03007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.Seats[0].IdentityCard.TakeDamage(damage);
        }, hero: false);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == apartment!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(Math.Max(0, damage - 1), world.Seats[0].IdentityCard.Damage);
        Assert.False(apartment!.Ready);
        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == apartment.ObjectId);
    }

    [Fact]
    [Rule("rr:lasting-effects.1")]
    public void FearlessDeterminationStacksDrawsAndExpiresDespiteEventsLeavingPlay()
    {
        // rr:lasting-effects.1: "whether or not the card that created the lasting effect is in play."
        Card[] events = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
            events = [board.CreateCard("03003", board.Seats[0].Hand), board.CreateCard("03003", board.Seats[0].Hand)]);
        int held = world.Seats[0].Hand.Cards.Count;
        foreach (var card in events)
        {
            var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == card.ObjectId);
            game.Resolve(Decision.Take(action.Id));
            Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        }
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(4, StateFields.Modified(world, world.Seats[0].IdentityCard, "thwart", world.Facts, world.Players));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(2, StateFields.Modified(world, world.Seats[0].IdentityCard, "thwart", world.Facts, world.Players));
    }
}
