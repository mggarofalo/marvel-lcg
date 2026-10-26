using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local printed 09034; no production book row or pack admission.</summary>
public sealed class SevenRingsRecipientTests
{
    internal const string Special = """
        {"seq":[{"chooseStatusRecipients":{"from":{"query":"characters"},"status":"tough","maximum":3}},
          {"discardInvocation":"this"}]}
        """;

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 3)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 3)]
    [InlineData(true, true, 3)]
    [Rule("rr:choose-game-element.3.1")]
    public void CompleteDistinctSubsetPrecedesStatusAndIndependentPhysicalDiscard(bool master, bool last, int count)
    {
        // "The same target cannot be chosen multiple times this way."
        // Printed 09034: "Give up to 3 characters each a tough status card."
        var (world, game, deck, _) = BoardWithSpecial(Special, last, "09034", master ? 1 : 0);
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var other = world.Seats[0].IdentityCard;
        var original = deck.PublicTop!;
        var next = last ? null : deck.Deck.Cards[^2];
        var physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order().ToArray();
        var first = game.Resolve(Payment(world, game, master, original));
        Assert.False(game.IsRootPrompt);
        Assert.Equal(1, game.Pending!.Player);
        var offer = Assert.Single(game.Pending.Affordances);
        Assert.Equal(0, offer.Targets!.Min);
        Assert.Equal(3, offer.Targets.Max);
        Assert.False(offer.Targets.AllowRepeated);
        Assert.Contains(villain.ObjectId, offer.Targets.Legal);
        Assert.Contains(other.ObjectId, offer.Targets.Legal);
        Assert.Contains(hero.ObjectId, offer.Targets.Legal);
        Assert.Contains("tough", game.Pending.Description);
        Assert.Contains("none selected", game.Pending.Description);
        Assert.Equal(original.ObjectId, Assert.Single(game.Pending.ContextCardIds));
        Assert.Same(original, deck.PublicTop);
        Assert.All(new[] { hero, villain, other }, card => Assert.Equal(0, Statuses.Count(world, card, Statuses.Tough)));
        string before = Snapshot(world);
        var pending = game.Pending;
        // Query purity includes RNG, pending work and a repeated action query.
        _ = game.Pending;
        _ = offer.Targets.Allows([]);
        _ = offer.Targets.Allows([hero.ObjectId, hero.ObjectId]);
        Assert.Equal(before, Snapshot(world));
        Assert.Same(pending, game.Pending);
        int[] selected = new[] { villain.ObjectId, other.ObjectId, hero.ObjectId }.Take(count).ToArray();
        var second = game.Resolve(new Decision(offer.Id, selected));
        var events = first.Events.Concat(second.Events).ToList();
        Assert.All(new[] { hero, villain, other }, card =>
            Assert.Equal(selected.Contains(card.ObjectId) ? 1 : 0, Statuses.Count(world, card, Statuses.Tough)));
        Assert.Equal(count, events.OfType<CardAttached>().Count(item => item.Verb == "Give_Status"));
        AssertInvocationLifecycle(events, original, deck, next, master, last);
        Assert.Equal(master, hero.Ready);
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("tooMany")]
    [InlineData("nonCharacter")]
    [InlineData("stale")]
    [InlineData("wrongOffer")]
    [InlineData("resources")]
    public void InvalidGroupPreservesCurrentChoiceAndEntireResolution(string kind)
    {
        var (world, game, deck, _) = BoardWithSpecial(Special, false, "09034", 0);
        var extra = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        game.Resolve(Payment(world, game, false, deck.PublicTop!));
        var offer = Assert.Single(game.Pending!.Affordances);
        var ids = offer.Targets!.Legal;
        Assert.Equal(4, ids.Count);
        int[] selected = kind switch
        {
            "duplicate" => [ids[0], ids[0]],
            "tooMany" => ids.Take(4).ToArray(),
            "nonCharacter" => [deck.PublicTop!.ObjectId],
            "stale" => [extra.ObjectId],
            _ => [ids[0]],
        };
        if (kind == "stale") World.MoveToTop(extra, world.Seats[1].Hand);
        string before = Snapshot(world);
        Assert.ThrowsAny<ArgumentException>(() => game.Resolve(new Decision(
            kind == "wrongOffer" ? -99 : offer.Id, selected,
            kind == "resources" ? [world.Seats[1].Hand.Cards[0].ObjectId] : [])));
        Assert.Equal(before, Snapshot(world));
        Assert.False(game.IsRootPrompt);
        Assert.Equal(1, game.Pending!.Player);
        game.Resolve(new Decision(offer.Id, []));
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:status-cards.1")]
    [Rule("rr:target.3.5")]
    public void ExistingToughAndCannotRestrictionLeaveOnlyIndependentDiscard(bool master)
    {
        // "A character cannot have more than one status card of each type at a time."
        // A target "is not valid" when another ability says it cannot perform the function.
        var (world, game, deck, _) = BoardWithSpecial(Special, false, "09034", master ? 1 : 0);
        foreach (var seat in world.Seats)
            world.CreateCard(Statuses.Tough, world.AreaOf(DeckType.StatusArea, host: seat.IdentityCard.ObjectId));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(
            Marvel.Rules.Timing.EffectSource.LastingEffect, "cannot_be_tough", Amount: 1,
            Card: villain.ObjectId, Affects: villain.ObjectId));
        Assert.Equal(0, Statuses.Limit(world, world.Facts, villain, Statuses.Tough));
        var original = deck.PublicTop!;
        var result = game.Resolve(Payment(world, game, master, original));
        Assert.DoesNotContain(result.Events.OfType<CardAttached>(), item => item.Verb == "Give_Status");
        Assert.Contains(result.Events.OfType<CardsMoved>(), item => item.Verb == "Discard"
            && item.Cards.Any(card => card.Card == original.ObjectId));
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Tough));
    }

    private static void AssertInvocationLifecycle(
        List<GameEvent> events, Card original, InvocationDeck deck, Card? next, bool master, bool last)
    {
        int discarded = events.FindIndex(item => item is CardsMoved move && move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        int revealed = events.FindIndex(discarded + 1, item => item is CardsFlipped flip && flip.Verb == "Reveal");
        Assert.True(discarded >= 0);
        Assert.True(revealed > discarded);
        Assert.Equal(last ? 1 : 0, events.OfType<AreaReordered>().Count());
        if (master)
        {
            int returned = events.FindIndex(item => item is CardsMoved move && move.Verb == "Return"
                && move.Cards.Any(card => card.Card == original.ObjectId));
            Assert.True(returned > revealed);
            Assert.Same(original, deck.PublicTop);
        }
        else if (!last)
        {
            Assert.Same(next, deck.PublicTop);
            Assert.Same(deck.Discard, original.Area);
        }
    }

    internal static Decision Payment(World world, Game game, bool master, Card original)
    {
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId)
            : Mastery(game, world.Seats[1].IdentityCard);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        return new(offer.Id, [original.ObjectId], sources,
            Allocations: ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!);
    }
}
