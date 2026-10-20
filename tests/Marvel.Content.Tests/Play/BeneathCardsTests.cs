using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BeneathCardsTests
{
    [Fact]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void AuthoredStorageSelectsOnlyItsPrintedCandidatesAndCanRetrieveThem()
    {
        Card? source = null;
        Card? candidate = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
                {"storeBeneath":{"cards":{"cardsIn":{"area":"yourDeck","title":"Swinging Web Kick"}},"host":"this","randomCount":1}},
                {"returnToHand":{"query":"beneathThis"}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            candidate = board.CreateCard("01005", board.Seats[0].Deck);
        }, abilities: runner);
        var before = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(BeneathCards.Of(world, source!));
        Assert.Contains(world.Seats[0].Hand.Cards, card => card.FaceId == candidate!.FaceId);
        Assert.False(source!.Ready);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void StoredCardsRetainTheirOwnerAndAreOutOfPlayRegardlessOfFaceVisibility(bool faceUp)
    {
        // Facedown cards attached to in-play cards are out of play (rr:in-play-and-out-of-play.10).
        // ruling:6768213a7e0f4606 likewise says faceup cards attached to Quiver are not in play.
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"]);
        var host = InPlay(world, AuthoredCards.AuntMay);
        var stored = world.Seats[1].Hand.Cards[0];
        var events = new List<GameEvent>();

        BeneathCards.Place(world, stored, host, faceUp, "store", events);

        Assert.False(DeckTypes.IsInPlay(stored.Area.Type));
        Assert.Equal(faceUp, stored.FaceUp);
        Assert.Equal(1, stored.Owner);
        Assert.Equal(host.ObjectId, stored.Area.Host);
        Assert.Equal([stored], BeneathCards.Of(world, host));
        Assert.Single(events.OfType<CardAttached>());
        Assert.DoesNotContain(stored, world.Seats[1].Hand.Cards);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:attach-to.1")]
    public void HostDepartureDiscardsStoredCardsToTheirPhysicalOwnersPile(bool faceUp)
    {
        // ruling:1dd3b64874d189f2 applies rr:attach-to.1 to out-of-play stored cards too:
        // "a card attached to another game element will get discarded when the element leaves play".
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"]);
        var host = InPlay(world, AuthoredCards.AuntMay);
        var stored = world.Seats[1].Hand.Cards[0];
        BeneathCards.Place(world, stored, host, faceUp, "store", []);

        Discard.Card(world, host, "discard", []);

        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1), stored.Area);
        Assert.Empty(BeneathCards.Of(world, host));
        Assert.True(stored.FaceUp);
        Assert.Equal(1, stored.Owner);
    }
}
