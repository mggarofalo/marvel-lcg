using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.CrimsonIndependentDiscardFixture;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent nonpublic review of the printed Crimson composition.</summary>
public sealed class CrimsonIndependentDiscardReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3.4")]
    [Rule("rr:target.3.6")]
    public void AValidMinionIsChosenWithoutSubstitutingTheProtectedVillain(bool master)
    {
        // A target is valid if "at least one of those effects can affect the target";
        // damage "dealt but not taken" still affects it. Tough cancels ordinary damage.
        var (world, game, deck, _) = BoardActualWithRunner(Runner(true), "09032", master);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var villainStun = Statuses.Give(world, villain, Statuses.Stunned);
        var minion = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Statuses.Give(world, minion, Statuses.Tough);
        var original = deck.PublicTop!;
        var events = Initiate(world, game, deck, master);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(minion.ObjectId, Assert.Single(game.Pending.Affordances).AnchorId);

        events.AddRange(game.Resolve(Decision.Take(minion.ObjectId)).Events);

        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.StatusArea, villainStun.Area.Type);
        Assert.Equal(1, Statuses.Count(world, minion, Statuses.Stunned));
        Assert.Equal(0, Statuses.Count(world, minion, Statuses.Tough));
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        AssertLifecycle(world, game, deck, original, events, master, false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ANewlyIneffectiveEnemyCannotConsumeThePendingChoice(bool master)
    {
        var (world, game, deck, _) = BoardActualWithRunner(Runner(true), "09032", master);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var original = deck.PublicTop!;
        _ = Initiate(world, game, deck, master);
        Assert.Equal(villain.ObjectId, Assert.Single(game.Pending!.Affordances).AnchorId);
        Statuses.Give(world, villain, Statuses.Stunned);
        var prompt = game.Pending;
        string before = Snapshot(world);

        Assert.ThrowsAny<Exception>(() => game.Resolve(Decision.Take(villain.ObjectId)));

        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, Snapshot(world));
        Assert.Same(original, deck.PublicTop);
        Assert.Equal(0, villain.Damage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:ability.4")]
    public void EmptySentencePreservesExactOwnedDiscardRevealRecycleAndReturn(bool master, bool last)
    {
        // Resolve "one sentence at a time". Ruling b07d53e12625ab23 requires the
        // actual Invocation discard, next-top disclosure, recycle if empty, then return.
        var (world, game, deck, _) = BoardActualWithRunner(Runner(true), "09032", master, last);
        Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
        var original = deck.PublicTop!;
        int[] physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order().ToArray();
        int? next = last ? null : deck.Deck.Cards[^2].ObjectId;

        var events = Initiate(world, game, deck, master);

        AssertLifecycle(world, game, deck, original, events, master, last);
        var discard = Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Equal(Places.Reference(deck.Deck), discard.From);
        Assert.Equal(Places.Reference(deck.Discard), discard.To);
        var reveals = events.OfType<CardsFlipped>().Where(flip => flip.FaceUp).ToArray();
        Assert.Equal(master ? 2 : 1, reveals.Length);
        int revealed = Assert.Single(reveals[0].Cards);
        Assert.Contains(revealed, physical);
        if (next is { } expected) Assert.Equal(expected, revealed);
        if (last)
        {
            var shuffle = Assert.Single(events.OfType<AreaReordered>());
            Assert.Equal(Places.Reference(deck.Deck), shuffle.Area);
            Assert.Equal(physical, shuffle.Order.Order());
            Assert.Equal(shuffle.Order[^1], revealed);
            Assert.True(events.IndexOf(discard) < events.IndexOf(shuffle));
            Assert.True(events.IndexOf(shuffle) < events.IndexOf(reveals[0]));
            var resets = events.OfType<CardsMoved>().Where(move => move.Verb == "Reset").ToArray();
            Assert.Equal(physical, resets.SelectMany(move => move.Cards).Select(card => card.Card).Order());
            Assert.All(resets, move => Assert.Equal(Places.Reference(deck.Deck), move.To));
        }
        if (master)
        {
            var returned = Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Return");
            Assert.Equal(original.ObjectId, Assert.Single(returned.Cards).Card);
            Assert.Equal(Places.Reference(deck.Deck), returned.To);
            Assert.Equal(original.ObjectId, Assert.Single(reveals[1].Cards));
            Assert.True(events.IndexOf(reveals[0]) < events.IndexOf(returned));
            Assert.True(events.IndexOf(returned) < events.IndexOf(reveals[1]));
        }
        else Assert.DoesNotContain(events, item => item.Verb == "Return");
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.All(deck.Deck.Cards.Concat(deck.Discard.Cards), card => Assert.Equal(1, card.Owner));
        Assert.Null(world.Seats[0].InvocationDeck);
    }
}
