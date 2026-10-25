using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaAvengersAssembleTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:max-maximum")]
    [Rule("rr:lasting-effects.4")]
    public void ActualEventReadiesOnlyControlledAvengersButGrantsBothSeatsUntilPhaseEnd(bool exhausted)
    {
        // Maximum applies across copies; later entrants are still affected by a lasting effect.
        Card? first = null;
        Card? second = null;
        Card? own = null;
        Card? foreign = null;
        Card? nonAvenger = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            first = board.CreateCard("03015", board.Seats[0].Hand);
            second = board.CreateCard("03015", board.Seats[0].Hand);
            payment = [board.CreateCard("03022", board.Seats[0].Hand).ObjectId,
                board.CreateCard("03023", board.Seats[0].Hand).ObjectId];
            board.CreateCard("03022", board.Seats[0].Hand);
            board.CreateCard("03023", board.Seats[0].Hand);
            own = Ally(board, "03014", 0);
            foreign = Ally(board, "03013", 1);
            nonAvenger = Ally(board, "01002", 0);
            foreign.Exhaust(); nonAvenger.Exhaust();
            if (exhausted) { own.Exhaust(); board.Seats[0].IdentityCard.Exhaust(); }
        }, twoPlayers: true);
        var play = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == first!.ObjectId && offer.PlaysCard);
        game.Resolve(new Decision(play.Id, [], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.True(own!.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(foreign!.Ready);
        Assert.False(nonAvenger!.Ready);
        Assert.Equal(4, Value(world, own, "attack"));
        Assert.Equal(2, Value(world, own, "thwart"));
        Assert.Equal(2, Value(world, foreign, "attack"));
        Assert.Equal(1, Value(world, nonAvenger, "attack"));
        Assert.Equal(DeckType.DiscardPile, first!.Area.Type);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == second!.ObjectId && offer.PlaysCard);
        var later = Ally(world, "03013", 0);
        Assert.Equal(2, Value(world, later, "attack"));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(3, Value(world, own, "attack"));
        Assert.Equal(1, Value(world, foreign, "attack"));
        Assert.Equal(1, Value(world, later, "attack"));
    }

    [Fact]
    public void AlterEgoCannotPlayTheHeroAction()
    {
        Card? card = null;
        var (game, _) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            card = board.CreateCard("03015", board.Seats[0].Hand);
            board.CreateCard("03022", board.Seats[0].Hand);
            board.CreateCard("03023", board.Seats[0].Hand);
        }, hero: false);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == card!.ObjectId && offer.PlaysCard);
    }

    private static Card Ally(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static long Value(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, world.Facts, world.Players);
}
