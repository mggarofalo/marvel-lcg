using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NamedReturnCostTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.7")]
    public void ReturnsNamedControlledCardBeforeDrawingWithoutReturningSource(bool exhausted)
    {
        // Synthetic Core fixture for Shield Toss's printed named return cost.
        // rr:cost.7: "that player must pay costs with cards and/or game elements they control."
        Card? source = null;
        Card? payment = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"returnTitledToHand":"Helicarrier"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            payment = InPlay(board, "01092");
            if (exhausted) payment.Exhaust();
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Same(world.Seats[0].Hand, payment!.Area);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("other-controller")]
    [InlineData("out-of-play")]
    [InlineData("ambiguous")]
    [Rule("rr:cost.7")]
    public void UnpayableNamedReturnNeverOffersOrMutates(string reason)
    {
        // rr:cost.7: costs require controlled cards. Ambiguous binding is an engine choice.
        Card? source = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"returnTitledToHand":"Helicarrier"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            if (reason == "missing") return;
            var area = reason == "out-of-play" ? board.Seats[0].Hand
                : board.AreaOf(DeckType.SupportsArea, PlayArea.Of(reason == "other-controller" ? 1 : 0), cardOwner: 0);
            board.CreateCard("01092", area);
            if (reason == "ambiguous") board.CreateCard("01092", area);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        string before = world.Digest().Canonical();

        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Fact]
    [Rule("rr:ownership-and-control")]
    public void ControlledBorrowedCardReturnsToOwnerAndDiscardsAttachments()
    {
        // rr:ownership-and-control: a card leaving play goes to its owner's equivalent out-of-play area.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"returnTitledToHand":"Helicarrier"}""");
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var source = InPlay(world, AuthoredCards.AuntMay);
        var payment = world.CreateCard("01092", world.Seats[1].Hand);
        World.MoveToTop(payment, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("01069", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), payment.ObjectId, 0));
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);

        runner.ExecuteAction(world, new PlayerAction(action, [], []));

        Assert.Same(world.Seats[1].Hand, payment.Area);
        Assert.Equal(DeckType.DiscardPile, attachment.Area.Type);
        Assert.Equal(0, attachment.Owner);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void StaleNamedDepartureRejectsBeforeEffect()
    {
        // rr:initiating-abilities.step.5: if costs cannot be paid, "abort this process".
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"returnTitledToHand":"Helicarrier"}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, AuthoredCards.AuntMay);
        var payment = InPlay(world, "01092");
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Discard.Card(world, payment, "test", []);
        string before = world.Digest().Canonical();

        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
        Assert.Equal(before, world.Digest().Canonical());
    }
}
