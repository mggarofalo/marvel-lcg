using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaReprintTests
{
    [Theory]
    [InlineData("decline")]
    [InlineData("villain")]
    [InlineData("minion")]
    [Rule("rr:ability.11")]
    public void MockingbirdOffersAnOptionalEnemyChoiceAfterActualPlay(string answer)
    {
        // rr:ability.11: "Unless prefaced by the word “Forced”, all interrupt and response abilities are optional."
        Card? ally = null;
        Card? minion = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            ally = board.CreateCard("03020", board.Seats[0].Hand);
            payment = [board.CreateCard("03021", board.Seats[0].Hand).ObjectId,
                board.CreateCard("01093", board.Seats[0].Hand).ObjectId];
            minion = board.CreateCard("03029", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        }, twoPlayers: true);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally!.ObjectId && item.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        if (answer == "decline") game.Resolve(Decision.Decline);
        else
        {
            var response = Assert.Single(game.Pending.Affordances, item => item.AnchorId == ally.ObjectId);
            game.Resolve(Decision.Take(response.Id));
            Assert.Contains(game.Pending!.Affordances, item => item.AnchorId == villain.ObjectId);
            Assert.Contains(game.Pending.Affordances, item => item.AnchorId == minion!.ObjectId);
            var target = answer == "villain" ? villain : minion!;
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances, item => item.AnchorId == target.ObjectId).Id));
        }
        Assert.Equal(answer == "villain", Statuses.Has(world, villain, Statuses.Stunned));
        Assert.Equal(answer == "minion", Statuses.Has(world, minion!, Statuses.Stunned));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData("03021", "YY")]
    [InlineData("03022", "BB")]
    [InlineData("03023", "RR")]
    public void PrintedDoubleResourcesPayForAnActualSupport(string face, string resources)
    {
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(_ => { });
        var runner = AuthoredCards.Runner();
        var tower = world.CreateCard("03024", world.Seats[0].Hand);
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        var generator = Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, tower),
            item => item.Effect == payment.ObjectId);
        Assert.Equal(resources, generator.Generates);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], tower, [payment.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, tower.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Theory]
    [InlineData("01067", "GG")]
    [InlineData("03020", "G")]
    [InlineData("03024", "G")]
    public void PowerOfLeadershipDoublesOnlyForTheLeadershipCardBeingPaid(string target, string resources)
    {
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(_ => { });
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard(target, world.Seats[0].Hand);
        var power = world.CreateCard("03018", world.Seats[0].Hand);
        var generator = Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, card),
            item => item.Effect == power.ObjectId);
        Assert.Equal("G", generator.Generates);
        Assert.Equal(resources, generator.PossibleResources);
        Assert.Equal(resources == "GG" ? new ConditionalResourceBonus("G", 1) : null,
            generator.ConditionalBonus);
    }
}
