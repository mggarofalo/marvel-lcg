using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaEnragedTests
{
    [Fact]
    [Rule("rr:attach-to.1")]
    public void ASecondCopyCannotAttachToTheSameAllyButCanAttachAcrossPlayers()
    {
        // rr:attach-to.1: "it must be attached to (placed beneath and slightly overlapped by) the specified game element as it enters play."
        Card? next = null;
        Card? owned = null;
        Card? other = null;
        Card? payment = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            owned = board.CreateCard("03012", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            other = board.CreateCard("03020", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
            board.CreateCard("03031", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), owned.ObjectId, 0));
            next = board.CreateCard("03031", board.Seats[0].Hand);
            payment = board.CreateCard("01093", board.Seats[0].Hand);
        }, twoPlayers: true);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == next!.ObjectId && item.PlaysCard);
        Assert.DoesNotContain(owned!.ObjectId, play.Targets!.Legal);
        Assert.DoesNotContain(world.Seats[0].IdentityCard.ObjectId, play.Targets.Legal);
        Assert.Contains(other!.ObjectId, play.Targets.Legal);
        int[] paid = [payment!.ObjectId];
        game.Resolve(new Decision(play.Id, [other.ObjectId], paid,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], paid)));
        Assert.Equal(other.ObjectId, next!.Area.Host);
        Assert.Equal(0, next.Owner);
        Assert.Equal(3, StateFields.Modified(world, other, "attack", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:consequential-damage")]
    public void ActualAttachmentChangesOnlyAttackAndItsConsequentialDamage(bool thwart)
    {
        // rr:consequential-damage: "After an ally attacks, it takes consequential damage equal to the number of consequential damage icons" beneath ATK.
        Card? enraged = null;
        Card? ally = null;
        Card? payment = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            enraged = board.CreateCard("03031", board.Seats[0].Hand);
            payment = board.CreateCard("01093", board.Seats[0].Hand);
            ally = board.CreateCard("03012", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        });
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == enraged!.ObjectId && item.PlaysCard);
        int[] paid = [payment!.ObjectId];
        game.Resolve(new Decision(play.Id, [ally!.ObjectId], paid,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], paid)));
        Assert.Equal(ally.ObjectId, enraged!.Area.Host);
        Assert.Equal(3, StateFields.Modified(world, ally, "attack", world.Facts, world.Players));
        Assert.Equal(1, StateFields.Modified(world, ally, "thwart", world.Facts, world.Players));
        var target = world.TheCardIn(thwart ? DeckType.MainSchemesArea : DeckType.VillainArea)!;
        BasicPowers.AllyPower(world, world.Facts, ally, target,
            thwart ? BasicPowers.ThwartVerb : BasicPowers.AttackVerb, []);
        Agendas.Finish(world, world.Facts, AuthoredCards.Runner());
        Assert.Equal(thwart ? 1 : 2, ally.Damage);
        Assert.Equal(thwart ? 4 : 5, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(thwart ? 0 : 3, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Discard.Card(world, enraged, "test", []);
        Assert.Equal(1, StateFields.Modified(world, ally, "attack", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, ally, "attack_consequential_damage", world.Facts, world.Players));
    }
}
