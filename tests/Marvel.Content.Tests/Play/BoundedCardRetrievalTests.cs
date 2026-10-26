using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BoundedCardRetrievalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void BrunoSelectsOneExactOptionalGroupAfterExhausting(int count)
    {
        // Printed 05007: exhaust Bruno, then add up to 3 attached cards to your hand.
        // rr:in-play-and-out-of-play.10: "Facedown cards attached to in-play cards are out of play."
        Card host = null!;
        Card[] stored = [];
        var runner = Runner("05007", "Action", """
            {"seq":[{"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}},
                {"draw":{"player":"you","count":1}}]}
            """, cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            stored = Enumerable.Range(0, 4).Select(_ => board.CreateCard("01005", board.Seats[0].Hand)).ToArray();
            foreach (var card in stored) BeneathCards.Place(board, card, host, false, "store", []);
        }, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == host.ObjectId).Id));
        Assert.False(host.Ready);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        var choice = Assert.Single(game.Pending!.Affordances);
        Assert.True(choice.Targets!.IsSearch);
        Assert.Equal(0, choice.Targets.Min);
        Assert.Equal(3, choice.Targets.Max);
        Assert.Equal(stored.Select(card => card.ObjectId), choice.Targets.Legal);
        var selected = stored.AsEnumerable().Reverse().Take(count).ToArray();

        game.Resolve(new Decision(choice.Id, selected.Select(card => card.ObjectId).ToArray()));

        Assert.Equal(hand + count + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(4 - count, BeneathCards.Of(world, host).Count);
        Assert.All(selected, card => { Assert.Same(world.Seats[0].Hand, card.Area); Assert.True(card.FaceUp); Assert.Equal(0, card.Owner); });
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.Label == "chooseCardsToHand");
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("tooMany")]
    [InlineData("otherHost")]
    [InlineData("hand")]
    [InlineData("unknown")]
    public void MalformedGroupLeavesEveryCardAndPromptUnchangedAndCanRetry(string kind)
    {
        Card host = null!;
        Card other = null!;
        Card[] cards = [];
        var runner = Runner("05007", "Action", """{"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}}""", cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            other = InPlay(board, AuthoredCards.AuntMay);
            cards = Enumerable.Range(0, 5).Select(_ => board.CreateCard("01005", board.Seats[0].Hand)).ToArray();
            foreach (var card in cards.Take(4)) BeneathCards.Place(board, card, host, false, "store", []);
            BeneathCards.Place(board, cards[4], other, false, "store", []);
        }, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == host.ObjectId).Id));
        var prompt = game.Pending!;
        var choice = Assert.Single(prompt.Affordances);
        int first = cards[0].ObjectId;
        int[] bad = kind switch
        {
            "duplicate" => [first, first],
            "tooMany" => cards.Take(4).Select(card => card.ObjectId).ToArray(),
            "otherHost" => [first, cards[4].ObjectId],
            "hand" => [first, world.Seats[0].Hand.Cards[0].ObjectId],
            _ => [first, int.MaxValue],
        };
        var before = System.Text.Json.JsonSerializer.Serialize(world.Digest());

        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(choice.Id, bad)));

        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(world.Digest()));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(4, BeneathCards.Of(world, host).Count);
        game.Resolve(new Decision(choice.Id, [first]));
        Assert.Same(world.Seats[0].Hand, cards[0].Area);
        Assert.Equal(3, BeneathCards.Of(world, host).Count);
    }
    [Fact]
    public void ForeignOwnedStoredCardEntersResolvingHandWithoutChangingOwner()
    {
        Card host = null!;
        Card foreign = null!;
        var runner = Runner("05007", "Action", """{"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}}""");
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            foreign = board.Seats[1].Hand.Cards[0];
            BeneathCards.Place(board, foreign, host, false, "store", []);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        world.FirstPlayer = 1;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == host.ObjectId).Id));
        Assert.Equal(0, game.Pending!.Player);
        Assert.NotEqual(world.FirstPlayer, game.Pending.Player);
        game.Resolve(new Decision(Assert.Single(game.Pending.Affordances).Id, [foreign.ObjectId]));
        Assert.Same(world.Seats[0].Hand, foreign.Area);
        Assert.Equal(1, foreign.Owner);
    }

    [Fact]
    [Rule("rr:initiating-abilities.3")]
    public void DepartedHostCanFinishOngoingRetrievalWithAnEmptyGroup()
    {
        Card host = null!;
        Card stored = null!;
        var runner = Runner("05007", "Action", """{"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}}""");
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            stored = board.Seats[0].Hand.Cards[0];
            BeneathCards.Place(board, stored, host, false, "store", []);
        }, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == host.ObjectId).Id));
        var choice = Assert.Single(game.Pending!.Affordances);
        // "the sequence does not stop from completing if that card leaves play".
        // Synthetic fixture mutation during a pending decision tests the execution
        // boundary; no legal gameplay interleaving here is claimed.
        Discard.Card(world, host, "depart", []);
        var before = System.Text.Json.JsonSerializer.Serialize(world.Digest());
        game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(world.Digest()));
        Assert.Equal(DeckType.DiscardPile, stored.Area.Type);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitiationRequiresAtLeastOneRetrievableCard(bool hasStoredCard)
    {
        Card host = null!;
        var runner = Runner("05007", "Action", """{"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}}""", cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            if (hasStoredCard) BeneathCards.Place(board, board.Seats[0].Hand.Cards[0], host, false, "store", []);
        }, abilities: runner);
        var offers = game.Pending!.Affordances.Where(offer => offer.AnchorId == host.ObjectId).ToList();
        Assert.Equal(hasStoredCard ? 1 : 0, offers.Count);
        Assert.True(host.Ready);
        Assert.Equal(hasStoredCard ? 1 : 0, BeneathCards.Of(world, host).Count);
    }

}
