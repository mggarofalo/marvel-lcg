using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent nonpublic Master continuation and exhaustion controls.</summary>
public sealed class MasterOfMysticArtsReviewTests
{
    [Fact]
    public void ActualMasterCanResolveWindsWhileDoctorStrangeIsExhausted()
    {
        var (world, game, deck, _) = Board(1, master: 1);
        var hero = world.Seats[1].IdentityCard;
        hero.Exhaust();
        var original = deck.PublicTop!;
        int hand = world.Seats[1].Hand.Cards.Count;

        game.Resolve(Payment(game, world, original));

        Assert.False(hero.Ready);
        Assert.Equal(hand + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Same(original, deck.PublicTop);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:then.1")]
    public void SyntheticSpecialStillReturnsWhenOnlyItsSelfDiscardChangesState()
    {
        // "If the pre-'then' text ... fully resolves", attempt the post-then text.
        // Master's predecessor instructs the Special procedure; its synthetic heal can do nothing.
        var (world, game, deck, _) = BoardWithSpecial("""
            {"seq":[{"heal":{"card":"you","amount":1}},{"discardInvocation":"this"}]}
            """, false);
        var original = deck.PublicTop!;
        var hero = world.Seats[1].IdentityCard;
        Assert.Equal(0, hero.Damage);
        int hand = world.Seats[1].Hand.Cards.Count;

        var result = game.Resolve(Payment(game, world, original));

        Assert.Equal(0, hero.Damage);
        Assert.Equal(hand - 2, world.Seats[1].Hand.Cards.Count);
        Assert.Contains(result.Events.OfType<CardsMoved>(), moved => moved.Verb == "Discard"
            && moved.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Contains(result.Events.OfType<CardsMoved>(), moved => moved.Verb == "Return"
            && moved.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Same(original, deck.PublicTop);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntheticChoiceAfterDiscardKeepsIntermediateTopPublicUntilOriginalReturns(bool last)
    {
        // Synthetic ordering control, not the actual Winds Special: discard, then choose a draw.
        var (world, game, deck, _) = BoardWithSpecial("""
            {"seq":[{"discardInvocation":"this"},{"choose":{"options":[
              {"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":3}}
            ]}}]}
            """, last);
        var original = deck.PublicTop!;
        long random = world.Random.Generator.WordsConsumed;
        int[] physical = [.. deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order()];

        var start = game.Resolve(Payment(game, world, original));

        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(2, game.Pending.Affordances.Count);
        Assert.DoesNotContain(start.Events, happened => happened.Verb == "Return");
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.True(deck.PublicTop!.FaceUp);
        Assert.Equal(last ? deck.Deck : deck.Discard, original.Area);
        Assert.Equal(last, world.Random.Generator.WordsConsumed > random);
        var intermediate = deck.PublicTop;
        if (!last) Assert.NotSame(original, intermediate);
        int hand = world.Seats[1].Hand.Cards.Count;

        var finish = game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));

        Assert.Equal(hand + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Same(original, deck.PublicTop);
        Assert.Single(finish.Events.OfType<CardsMoved>(), moved => moved.Verb == "Return"
            && moved.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        if (!ReferenceEquals(original, intermediate)) Assert.False(intermediate!.FaceUp);
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Decision Payment(Game game, Marvel.Rules.State.World world, Marvel.Rules.State.Card top)
    {
        var offer = Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        var allocations = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources));
        return new Decision(offer.Id, [top.ObjectId], sources, Allocations: allocations);
    }
}
