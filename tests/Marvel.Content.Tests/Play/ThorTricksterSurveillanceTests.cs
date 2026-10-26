using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorTricksterSurveillanceTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [Rule("rr:player-deck.3")]
    public void TricksterCountsExactDiscardTypesIncludingReset(bool reset, bool sameType, int expected)
    {
        // "no further cards are discarded from the newly shuffled deck."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[1];
        foreach (var card in seat.Deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var resource = world.CreateCard("06022", seat.Deck);
        var second = world.CreateCard(sameType ? "06023" : "06007", reset
            ? world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1) : seat.Deck);
        var third = world.CreateCard("06024", reset ? second.Area : seat.Deck);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var fixture = new RedSkullFixture(world, runner);
        fixture.Reveal("06030", 1);
        fixture.Finish();
        Assert.Equal(expected, scheme.Tokens["k_threat"]);
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
        Assert.Empty(seat.Hand.Cards);
        if (reset)
        {
            Assert.Equal(3, seat.Deck.Cards.Count);
            Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        }
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(CardKind.Resource, world.Facts.Kind(resource.FaceId));
        Assert.Equal(sameType ? CardKind.Resource : CardKind.Support, world.Facts.Kind(second.FaceId));
        Assert.Equal(CardKind.Resource, world.Facts.Kind(third.FaceId));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void SurveillanceAddsFourTotalAndEndsOnDeparture(int players)
    {
        var world = ThorActualFixture.Board("06001a", players);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long printed = Modified(world, scheme, "target_threat");
        var card = world.CreateCard("06031", seat.Hand);
        var payment = world.CreateCard("06022", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, card, [payment.ObjectId], [], [scheme.ObjectId]);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(scheme.ObjectId, card.Area.Host);
        Assert.Equal(printed + 4, Modified(world, scheme, "target_threat"));
        var duplicate = world.CreateCard("06031", seat.Hand);
        world.CreateCard("06024", seat.Hand);
        Assert.Null(CardPayment.Price(world, world.Facts, seat, duplicate));
        Discard.Card(world, card, "test", []);
        Assert.Equal(printed, Modified(world, scheme, "target_threat"));
        Assert.NotNull(CardPayment.Price(world, world.Facts, seat, duplicate));
        Assert.Empty(world.Agenda.Outstanding);
    }
}
