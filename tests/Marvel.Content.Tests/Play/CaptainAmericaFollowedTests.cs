using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaFollowedTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:interrupt")]
    public void ActualAttachmentInterruptsSchemeDefeatBeforeDiscardAndCanBeDeclined(bool decline)
    {
        // rr:interrupt: an interrupt "interrupts the resolution" of its condition.
        // Followed: "When attached scheme is defeated, deal 4 damage to an enemy."
        Card? followed = null;
        Card? scheme = null;
        Card? payment = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            followed = board.CreateCard("03032", board.Seats[0].Hand);
            payment = board.CreateCard("01093", board.Seats[0].Hand);
            scheme = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", 2);
        });
        var play = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == followed!.ObjectId && offer.PlaysCard);
        int[] paid = [payment!.ObjectId];
        game.Resolve(new Decision(play.Id, [scheme!.ObjectId], paid,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], paid)));
        var thwart = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == BasicPowers.ThwartVerb && offer.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        game.Resolve(new Decision(thwart.Id, [scheme.ObjectId]));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(scheme.ObjectId, followed!.Area.Host);
        if (decline) game.Resolve(Decision.Decline);
        else
        {
            var response = Assert.Single(game.Pending.Affordances, offer => offer.AnchorId == followed.ObjectId);
            game.Resolve(new Decision(response.Id, [world.TheCardIn(DeckType.VillainArea)!.ObjectId]));
        }
        Assert.Equal(decline ? 0 : 4, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Equal(DeckType.DiscardPile, followed.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }
}
