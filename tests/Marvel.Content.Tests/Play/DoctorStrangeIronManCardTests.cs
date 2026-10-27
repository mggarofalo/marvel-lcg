using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Iron Man, Inspired and Enraged programs through paid card play.</summary>
public sealed class DoctorStrangeIronManCardTests
{
    [Theory]
    [InlineData("01074", true)]
    [InlineData("01074", false)]
    [InlineData("03031", true)]
    [InlineData("03031", false)]
    [Rule("rr:initiating-abilities.step.4")]
    public void SelectedPhysicalHostDeterminesPaymentAndActualAttachmentBonus(string face, bool discounted)
    {
        // "Apply any modifiers to the cost(s)." Printed 09039 reduces upgrades on this ally by one.
        Card? iron = null, other = null, upgrade = null, resource = null;
        var (game, world) = Playing(board =>
        {
            iron = Ally(board, "09039", 1);
            other = Ally(board, "01067", 0);
            upgrade = board.CreateCard(face, board.Seats[0].Hand);
            resource = board.CreateCard("01089", board.Seats[0].Hand);
        }, heroes: ["captain_marvel", "spider_man"], abilities: AuthoredCards.Runner());
        var host = discounted ? iron! : other!;
        var unselected = discounted ? other! : iron!;
        long attack = StateFields.Modified(world, host, "attack", world.Facts, world.Players);
        long thwart = StateFields.Modified(world, host, "thwart", world.Facts, world.Players);
        long otherAttack = StateFields.Modified(world, unselected, "attack", world.Facts, world.Players);
        var offer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == upgrade!.ObjectId && option.PlaysCard);
        Assert.Equal("0", Assert.Single(offer.CostOptions, cost => cost.Target == iron!.ObjectId).Cost);
        var normal = Assert.Single(offer.CostOptions, cost => cost.Target == other!.ObjectId);
        Assert.Equal("1", normal.Cost);
        Assert.False(DecisionAssessment.Assess(game.Pending, new Decision(offer.Id, [other!.ObjectId], [])).IsAccepted);
        int[] paid = discounted ? [] : [resource!.ObjectId];
        var cost = Assert.Single(offer.CostOptions, candidate => candidate.Target == host.ObjectId);
        game.Resolve(new Decision(offer.Id, [host.ObjectId], paid, Allocations: ResourcePayment.Allocate(cost, paid)));
        Assert.Equal(host.ObjectId, upgrade!.Area.Host);
        Assert.Equal(discounted ? 1 : 0, upgrade.Area.PlayArea.Player);
        Assert.Equal(0, upgrade.Owner);
        Assert.Equal(discounted ? world.Seats[0].Hand.Type : DeckType.DiscardPile, resource!.Area.Type);
        Assert.Equal(attack + (face == "01074" ? 1 : 2), StateFields.Modified(world, host, "attack", world.Facts, world.Players));
        Assert.Equal(thwart + (face == "01074" ? 1 : 0), StateFields.Modified(world, host, "thwart", world.Facts, world.Players));
        Assert.Equal(otherAttack, StateFields.Modified(world, unselected, "attack", world.Facts, world.Players));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    public void RepeatedDiscountsFloorAtZeroAndDoNotAffectOrdinaryUpgrades()
    {
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        var iron = Ally(world, "09039", 1);
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 2);
        foreach (string face in new[] { "01074", "03031" })
        {
            var upgrade = world.CreateCard(face, world.Seats[0].Hand);
            Assert.Equal(0, CardPayment.CostOf(world, world.Facts, world.Seats[0], upgrade, iron.ObjectId).Amount);
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], upgrade, [], [], [iron.ObjectId]);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(iron.ObjectId, upgrade.Area.Host);
        }
        Assert.Equal(5, StateFields.Modified(world, iron, "attack", world.Facts, world.Players));
        Assert.Equal(3, StateFields.Modified(world, iron, "thwart", world.Facts, world.Players));
        var ordinary = world.CreateCard("01008", world.Seats[0].Hand);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], ordinary, iron.ObjectId).Amount);
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Ally(World world, string face, int controller) => world.CreateCard(face,
        world.AreaOf(DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: controller));
}
