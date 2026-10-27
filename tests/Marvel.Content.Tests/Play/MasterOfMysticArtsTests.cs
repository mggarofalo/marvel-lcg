using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local actual Master/Winds and explicitly synthetic paid Special controls.</summary>
public sealed class MasterOfMysticArtsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("ruling:b07d53e12625ab23")]
    [Rule("rr:then.1")]
    public void ActualWindsDiscardsRevealsOrRecyclesThenReturnsTheSamePhysicalCard(bool last)
    {
        // "If the pre-'then' text ... fully resolves", attempt the post-then text.
        // The September 2025 ruling requires the intermediate reveal/reset before return.
        var (world, game, deck, _) = Board(1, last: last, master: last ? 3 : 1);
        if (!last)
            foreach (var card in deck.Deck.Cards.Take(3).ToArray()) World.MoveToTop(card, deck.Discard);
        var original = deck.PublicTop!;
        var other = deck.Deck.Cards[0];
        int[] physical = [.. deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order()];
        var hero = world.Seats[1].IdentityCard;
        int hand = world.Seats[1].Hand.Cards.Count;
        long random = world.Random.Generator.WordsConsumed;
        var offer = Offer(game, world);
        Assert.Equal("1", Assert.Single(offer.CostOptions).Cost);

        var result = game.Resolve(Pay(offer, original, world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088")));

        Assert.True(hero.Ready);
        Assert.Equal(hand - 2 + 3, world.Seats[1].Hand.Cards.Count);
        Assert.Same(original, deck.PublicTop);
        Assert.Equal(last ? 5 : 2, deck.Deck.Cards.Count);
        Assert.Equal(last ? 0 : 3, deck.Discard.Cards.Count);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        AssertLifecycle(world, deck, original, other, result.Events.ToList(), random, last);
        var events = result.Events.ToList();
        var played = Assert.IsType<CardsMoved>(Assert.Single(events, happened => happened.Verb == CardPlay.Verb));
        Assert.Equal("09005", world.Cards[Assert.Single(played.Cards).Card].FaceId);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.5")]
    public void SyntheticPaidSpecialCombinesCostsAndDiscountsOnlyTheEvent(bool discount)
    {
        // Multiple costs "must be paid simultaneously"; Crimson Bands' two-cost
        // draw/discard Special is synthetic. Actual Winds remains zero-cost.
        var (world, game, deck, _) = Board(1, paid: true, master: discount ? 2 : 1);
        var hero = world.Seats[1].IdentityCard;
        var top = deck.PublicTop!;
        var offer = Offer(game, world);
        Assert.Equal(discount ? "2" : "3", Assert.Single(offer.CostOptions).Cost);
        var sources = world.Seats[1].Hand.Cards.Where(card => card.FaceId == "01088").Take(discount ? 1 : 2).ToArray();
        if (!discount)
        {
            var prompt = game.Pending;
            string before = Snapshot(world);
            Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id, [top.ObjectId], [sources[0].ObjectId])));
            Assert.Equal(before, Snapshot(world));
            Assert.Same(prompt, game.Pending);
        }
        int hand = world.Seats[1].Hand.Cards.Count;
        game.Resolve(Pay(offer, top, sources));
        Assert.Equal(hand - sources.Length - 1 + 3, world.Seats[1].Hand.Cards.Count);
        Assert.True(hero.Ready);
        Assert.Same(top, deck.PublicTop);
        Assert.All(sources, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void SyntheticSpecialChoiceSuspendsBeforeDiscardAndResumesBeforeReturn()
    {
        var (world, game, deck, _) = Board(1, master: 1, choice: true);
        var original = deck.PublicTop!;
        int hand = world.Seats[1].Hand.Cards.Count;
        game.Resolve(Pay(Offer(game, world), original, world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088")));
        Assert.Same(original, deck.PublicTop);
        Assert.Empty(deck.Discard.Cards);
        Assert.Equal(hand - 2, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(2, game.Pending.Affordances.Count);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(hand - 2 + 3, world.Seats[1].Hand.Cards.Count);
        Assert.Same(original, deck.PublicTop);
        Assert.Empty(deck.Discard.Cards);
        Assert.Equal(5, deck.Deck.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("deeper")]
    [InlineData("foreign")]
    [InlineData("stale")]
    public void RejectingAnUncapturedTopLeavesCostsAndPromptUntouched(string change)
    {
        var (world, game, deck, _) = Board(1, master: 1);
        var offer = Offer(game, world);
        var original = deck.PublicTop!;
        int target = change == "deeper" ? deck.Deck.Cards[0].ObjectId
            : change == "foreign" ? world.Seats[0].Hand.Cards[0].ObjectId : original.ObjectId;
        if (change == "stale") deck.DiscardTop(original, "probe", []);
        var payment = world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088");
        var prompt = game.Pending;
        string before = Snapshot(world);
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id, [target], [payment.ObjectId])));
        Assert.Equal(before, Snapshot(world));
        Assert.Same(prompt, game.Pending);
    }

    [Theory]
    [InlineData("unchanged")]
    [InlineData("owner")]
    [InlineData("incarnation")]
    [InlineData("replacement")]
    public void SyntheticCombinedPaymentRetainsItsCapturedTopAcrossResourceInterrupt(string change)
    {
        var (world, game, deck, _) = Board(1, paid: true, live: true, master: 1);
        var original = deck.PublicTop!;
        var generator = world.Cards.Single(card => card.FaceId == "01030");
        var payment = world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088");
        int hand = world.Seats[1].Hand.Cards.Count;
        game.Resolve(Pay(Offer(game, world), original, generator, payment));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Same(original, deck.PublicTop);
        switch (change)
        {
            case "owner": original.TransferScenarioOwnership(0); break;
            case "incarnation":
                World.MoveToTop(original, world.Seats[1].Hero);
                World.MoveToTop(original, deck.Deck);
                original.TurnFaceUp();
                break;
            case "replacement": deck.DiscardTop(original, "probe", []); break;
        }
        if (change != "unchanged")
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
            Assert.Contains("captured Invocation top is no longer current", error.Message, StringComparison.Ordinal);
            Assert.Equal(hand - 2, world.Seats[1].Hand.Cards.Count);
            return;
        }
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.Equal(hand - 2 + 1 + 3, world.Seats[1].Hand.Cards.Count);
        Assert.Same(original, deck.PublicTop);
        Assert.True(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void MasterDoesNotFabricateAnUnauthoredActualInvocationSpecial()
    {
        var (world, game, deck, _) = Board(1, paid: true, unsupported: true, master: 1);
        var original = deck.PublicTop!;
        var sources = world.Seats[1].Hand.Cards.Where(card => card.FaceId == "01088").Take(2).ToArray();
        var error = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Pay(Offer(game, world), original, sources)));
        Assert.Contains("09032' has no authored Special ability", error.Message, StringComparison.Ordinal);
        Assert.Same(original, deck.PublicTop);
        Assert.Empty(deck.Discard.Cards);
        Assert.True(world.Seats[1].IdentityCard.Ready);
    }

    private static void AssertLifecycle(World world, InvocationDeck deck, Card original, Card other,
        List<GameEvent> events, long random, bool last)
    {
        int discarded = events.FindIndex(happened => happened is CardsMoved moved && moved.Verb == "Discard"
            && moved.Cards.Any(card => card.Card == original.ObjectId));
        int returned = events.FindIndex(happened => happened is CardsMoved moved && moved.Verb == "Return"
            && moved.Cards.Any(card => card.Card == original.ObjectId));
        Assert.True(discarded >= 0 && returned > discarded);
        var reveal = Assert.Single(events.Skip(discarded + 1).Take(returned - discarded - 1).OfType<CardsFlipped>(), flip => flip.FaceUp);
        if (last)
        {
            var shuffle = Assert.Single(events.OfType<AreaReordered>());
            Assert.NotEqual(original.ObjectId, shuffle.Order[^1]);
            Assert.Equal(shuffle.Order[^1], Assert.Single(reveal.Cards));
            Assert.Equal(shuffle.Order.Where(id => id != original.ObjectId).Append(original.ObjectId), deck.Deck.Cards.Select(card => card.ObjectId));
            Assert.True(world.Random.Generator.WordsConsumed > random);
        }
        else
        {
            Assert.Equal([other.ObjectId], reveal.Cards);
            Assert.Empty(events.OfType<AreaReordered>());
            Assert.Equal(random, world.Random.Generator.WordsConsumed);
        }
    }

    private static Affordance Offer(Game game, World world) => Assert.Single(game.Pending!.Affordances,
        item => item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId);

    private static Decision Pay(Affordance offer, Card top, params Card[] sources)
    {
        int[] paying = [.. sources.Select(card => card.ObjectId)];
        var allocations = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(offer.CostOptions), paying));
        return new Decision(offer.Id, [top.ObjectId], paying, Allocations: allocations);
    }
}
