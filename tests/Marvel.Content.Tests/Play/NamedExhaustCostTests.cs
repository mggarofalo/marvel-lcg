using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NamedExhaustCostTests
{
    [Fact]
    [Rule("rr:cost.7")]
    [Rule("rr:exhausted.2")]
    public void NamedCostExhaustsTheControlledCardRatherThanTheAbilitySource()
    {
        // Synthetic ability using Core cards proves the named-cost primitive.
        // rr:cost.7: "that player must pay costs with cards and/or game elements they control."
        Card? source = null;
        Card? payment = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"exhaustTitled":"Helicarrier"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            payment = InPlay(board, "01092");
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(payment!.Ready);
        Assert.True(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("exhausted")]
    [InlineData("other-controller")]
    [InlineData("out-of-play")]
    [Rule("rr:cost.7")]
    [Rule("rr:exhausted.2")]
    public void UnpayableNamedCostNeverOffersOrMutates(string reason)
    {
        // rr:exhausted.2: an exhausted card cannot be exhausted again until it is ready.
        Card? source = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"exhaustTitled":"Helicarrier"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            if (reason == "missing") return;
            var area = reason == "out-of-play" ? board.Seats[0].Hand
                : board.AreaOf(DeckType.SupportsArea, PlayArea.Of(reason == "other-controller" ? 1 : 0), cardOwner: 0);
            var payment = board.CreateCard("01092", area);
            if (reason == "exhausted") payment.Exhaust();
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(2, world.Seats.Count);
    }
}
