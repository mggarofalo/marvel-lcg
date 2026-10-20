using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaAgentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.11")]
    public void ActualAgentResponseIsOptionalAndRemovesTwoWithoutConsumingConfused(bool decline)
    {
        // rr:ability.11: "Unless prefaced by the word “Forced”, all interrupt and response abilities are optional."
        Card? agent = null;
        Card? main = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            agent = board.CreateCard("03002", board.Seats[0].Hand);
            payment = [board.CreateCard("01088", board.Seats[0].Hand).ObjectId,
                board.CreateCard("01093", board.Seats[0].Hand).ObjectId];
            main = board.TheCardIn(DeckType.MainSchemesArea)!;
            main.PlaceTokens("k_threat", 4);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
        });
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == agent!.ObjectId && item.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.Equal(DeckType.AlliesArea, agent!.Area.Type);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        if (decline) game.Resolve(Decision.Decline);
        else
        {
            var response = Assert.Single(game.Pending.Affordances, item => item.AnchorId == agent.ObjectId);
            game.Resolve(Decision.Take(response.Id));
            var target = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == main!.ObjectId);
            game.Resolve(Decision.Take(target.Id));
        }
        Assert.Equal(decline ? 4 : 2, main!.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }
}
