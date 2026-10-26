using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelNakiaSuitIndependentReviewTests
{
    [Fact]
    public void SuitUsesItsSecondPlayersFormRatherThanTheFirstPlayersForm()
    {
        // Printed 05009: "Hero Resource" is controlled by the Suit's player.
        var runner = AuthoredCards.Runner();
        Card? suit = null;
        var (_, world) = Playing(board => suit = board.CreateCard("05009",
            board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1)),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        world.Seats[1].IdentityCard.TurnTo("01010a");
        var mine = world.CreateCard("05003", world.Seats[1].Hand);
        var theirs = world.CreateCard("05003", world.Seats[0].Hand);
        Assert.Contains(CardPayment.Generators(world, world.Facts, world.Seats[1], runner, mine),
            source => source.Effect == suit!.ObjectId && source.Generates == "G");
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, theirs),
            source => source.Effect == suit!.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        world.Seats[1].IdentityCard.TurnTo("01010b");
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[1], runner, mine),
            source => source.Effect == suit!.ObjectId);
    }

    [Theory]
    [InlineData("01018", false)]
    [InlineData("01018", true)]
    [InlineData("05009", false)]
    [InlineData("05009", true)]
    public void ActualNextPlayConsumesNakiaAtZeroEvenAfterHerDeparture(string face, bool depart)
    {
        // Printed 05008: "the next card you play this phase" includes a zero-cost card.
        // Printed 01018 costs zero; 05009 costs one before Nakia's reduction.
        var runner = AuthoredCards.Runner();
        Card? nakia = null;
        var (_, world) = Playing(board => nakia = board.CreateCard("05008",
            board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == nakia!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0),
            item => item.Card == nakia!.ObjectId), [], []));
        if (depart) World.MoveToTop(nakia!, world.AreaOf(DeckType.DiscardPile,
            PlayArea.Of(0), cardOwner: 0));
        var next = world.CreateCard("05003", world.Seats[0].Hand);
        var foreign = world.CreateCard("01018", world.Seats[1].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[1], foreign, [], []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.UpgradesArea, foreign.Area.Type);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], next).Amount);
        var free = world.CreateCard(face, world.Seats[0].Hand);
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, world.Seats[0], free).Amount);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], free, [], []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.UpgradesArea, free.Area.Type);
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], next).Amount);
    }
}
