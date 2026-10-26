using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BoundedRetrievalAdmissionReviewTests
{
    [Fact]
    [Rule("rr:target.3")]
    [Rule("rr:target.3.3")]
    public void CardsAlreadyInResolvingHandCannotEstablishAnInitiationEffect()
    {
        // "A target is valid ... if any part of that ability can affect that target."
        // "The cost ... is not considered" when determining whether it can affect it.
        Card host = null!;
        var runner = Runner("05007", "Action", """
            {"chooseCardsToHand":{"from":{"cardsIn":{"area":"yourHand"}},"maximum":3}}
            """, cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board => host = InPlay(board, "05007"), abilities: runner);
        Assert.NotEmpty(world.Seats[0].Hand.Cards);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == host.ObjectId);
        Assert.True(host.Ready);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void HostLeavingAndReenteringCannotAnswerAnEarlierRetrieval()
    {
        // "When a card leaves play ... it is considered to be a new copy of the card."
        Card host = null!;
        Card stored = null!;
        var runner = Runner("05007", "Action", """
            {"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}}
            """, cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            stored = board.Seats[0].Hand.Cards[0];
            BeneathCards.Place(board, stored, host, false, "store", []);
        }, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == host.ObjectId).Id));
        var prompt = game.Pending!;
        Discard.Card(world, host, "depart", []);
        World.MoveToTop(host, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        BeneathCards.Place(world, stored, host, false, "store", []);
        var before = System.Text.Json.JsonSerializer.Serialize(world.Digest());
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(Assert.Single(prompt.Affordances).Id, [stored.ObjectId])));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(world.Digest()));
        Assert.Same(prompt, game.Pending);
    }
}
