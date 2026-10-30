using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent actual-row checks in prepared nonpublic execution scenes.</summary>
public sealed class MsMarvelResourceIndependentTests
{
    [Fact]
    [Rule("rr:you-your.1")]
    public void AamirUsesControllerRatherThanPhysicalOwnersDiscardAndDraw()
    {
        // rr:you-your.1: "all instances in that sentence refer to the same identity or player."
        // Printed 05006 selects "your discard pile" and draws for that resolving controller.
        Card? source = null;
        Card? selected = null;
        Card? foreign = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("05006", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
            selected = board.CreateCard("01004", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            foreign = board.CreateCard("01003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }, heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var top = world.Seats[1].Deck.Cards[^1];
        int firstHand = world.Seats[0].Hand.Cards.Count;
        int controllerHand = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var choice = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, choice, Decision.Take(selected!.ObjectId));
        Assert.Same(world.Seats[1].Deck, selected!.Area);
        Assert.Same(world.Seats[1].Hand, top.Area);
        Assert.Equal(firstHand, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(controllerHand + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, foreign!.Area.Type);
        Assert.Equal(0, source!.Owner);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NonFirstMansionControllerChoosesEitherPlayer(int recipient)
    {
        Card? source = null;
        var (_, world) = Playing(board => source = board.CreateCard("05022",
            board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0)),
            heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var top = world.Seats[recipient].Deck.Cards[^1];
        int[] held = world.Seats.Select(seat => seat.Hand.Cards.Count).ToArray();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        runner.Chose(world, Assert.Single(world.Agenda.Outstanding),
            Decision.Take(world.Seats[recipient].IdentityCard.ObjectId));
        Assert.Same(world.Seats[recipient].Hand, top.Area);
        for (int player = 0; player < 2; player++)
            Assert.Equal(held[player] + (player == recipient ? 1 : 0), world.Seats[player].Hand.Cards.Count);
        Assert.False(source!.Ready);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
    }

    [Fact]
    public void ProtectionResourceQualifiesAbilityCostOnProtectionCard()
    {
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var power = world.CreateCard("05016", world.Seats[0].Hand);
        var protection = world.CreateCard("01080", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal("GG", Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner,
            abilitySource: protection), item => item.Effect == power.ObjectId).PossibleResources);
        var card = world.CreateCard("01080", world.Seats[0].Hand);
        Assert.Equal("GG", Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, card),
            item => item.Effect == power.ObjectId).PossibleResources);
        Assert.Equal("G", Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner),
            item => item.Effect == power.ObjectId).Generates);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    [Rule("rr:ownership-and-control.7.2")]
    [Rule("rr:uses-x-type")]
    public void NonFirstReflexesControllerSpendsLastCounterAndDiscardsToPhysicalOwner()
    {
        // rr:resource-ability.1: "the player who controls the ability" pays the cost.
        // rr:uses-x-type: "When the last all-purpose counter is removed from a card with uses, discard that card."
        // rr:ownership-and-control.7.2: "it is placed in its owner's equivalent out-of-play area".
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            board.Seats[1].IdentityCard.TurnTo("01010a");
            source = board.CreateCard("05024", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
            source.PlaceTokens("c_energy", 1);
        }, heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard("01026", world.Seats[1].Hand);
        Assert.Equal("Y", Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[1], runner, card),
            item => item.Effect == source!.ObjectId).Generates);
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, card),
            item => item.Effect == source!.ObjectId);
        CardPlay.Play(world, world.Facts, runner, world.Seats[1], card, [source!.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, card.Area.Type);
        Assert.Equal(PlayArea.Of(1), card.Area.PlayArea);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0), source.Area);
        Assert.Equal(0, source.Owner);
    }
}
