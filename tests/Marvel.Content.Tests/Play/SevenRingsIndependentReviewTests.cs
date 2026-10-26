using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent printed Seven Rings evidence and synthetic generic-status controls.</summary>
public sealed class SevenRingsIndependentReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:choose-game-element.3.1")]
    public void ExactlyTwoRecipientsPublishPhysicalStatusRelationshipsBeforeSelfDiscard(bool master, bool last)
    {
        // Printed 09034: "Give up to 3 characters each a tough status card."
        // "The same target cannot be chosen multiple times this way."
        var (world, game, deck, _) = BoardWithSpecial(SevenRingsRecipientTests.Special, last, "09034", master ? 1 : 0);
        var original = deck.PublicTop!;
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(SevenRingsRecipientTests.Payment(world, game, master, original));
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(1, game.Pending.Player);
        Assert.Same(original, deck.PublicTop);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Tough));
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Tough));

        var result = game.Resolve(new Decision(offer.Id, new[] { hero.ObjectId, villain.ObjectId }.OrderDescending().ToArray()));

        var given = result.Events.Where(item => item.Verb == "Give_Status").ToArray();
        Assert.Equal(new[] { typeof(CardsCreated), typeof(CardAttached), typeof(CardsCreated), typeof(CardAttached) },
            given.Select(item => item.GetType()));
        Assert.Equal(new[] { hero.ObjectId, villain.ObjectId }.Order(), given.OfType<CardAttached>().Select(item => item.Host));
        Assert.All(given.OfType<CardAttached>(), attached =>
        {
            var created = Assert.Single(given.OfType<CardsCreated>(), item => item.Cards.Any(card => card.Id == attached.Card));
            Assert.Equal(Statuses.Tough, Assert.Single(created.Cards).Card);
            Assert.Equal(created.Trigger, attached.Trigger);
            Assert.False(string.IsNullOrEmpty(attached.Trigger));
            Assert.Equal(attached.Host, world.Cards[attached.Card].Area.Host);
        });
        int discard = result.Events.ToList().FindIndex(item => item is CardsMoved moved && moved.Verb == "Discard"
            && moved.Cards.Any(card => card.Card == original.ObjectId));
        Assert.True(discard > result.Events.ToList().FindLastIndex(item => item.Verb == "Give_Status"));
        Assert.Single(result.Events.OfType<CardsMoved>(), item => item.Verb == "Discard"
            && item.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Equal(last ? 1 : 0, result.Events.OfType<AreaReordered>().Count());
        Assert.Equal(1, Statuses.Count(world, hero, Statuses.Tough));
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Tough));
        Assert.Equal(0, Statuses.Count(world, world.Seats[1].IdentityCard, Statuses.Tough));
        Assert.Equal(master ? 1 : 0, result.Events.OfType<CardsMoved>().Count(item => item.Verb == "Return"
            && item.Cards.Any(card => card.Card == original.ObjectId)));
        if (master) Assert.Same(original, deck.PublicTop);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("tough", "steady", 1, false)]
    [InlineData("tough", "stalwart", 0, true)]
    [InlineData("tough", "stalwart", 1, false)]
    [InlineData("confused", "steady", 1, true)]
    [InlineData("stunned", "steady", 1, true)]
    [InlineData("confused", "steady", 2, false)]
    [InlineData("stunned", "steady", 2, false)]
    [InlineData("confused", "stalwart", 0, false)]
    [InlineData("stunned", "stalwart", 0, false)]
    [InlineData("tough", "cannot_be_tough", 0, false)]
    [InlineData("confused", "cannot_be_confused", 0, false)]
    [InlineData("stunned", "cannot_be_stunned", 0, false)]
    [InlineData("confused", "", 1, false)]
    [InlineData("stunned", "", 1, false)]
    [Rule("rr:status-cards.1")]
    [Rule("rr:status-cards.1.1")]
    [Rule("rr:stalwart.1")]
    [Rule("rr:target.3.5")]
    public void SyntheticGroupUsesTheRequestedStatusLimit(string status, string modifier, int existing, bool eligible)
    {
        // "A character cannot have more than one status card of each type at a time."
        // Steady permits "one additional confused status card and one additional stunned status card."
        // Stalwart: "This character cannot have confused or stunned status cards."
        // A target is invalid if another ability says it "cannot perform" the function.
        // Synthetic Special on another face proves the generic instruction, not printed Seven Rings.
        string special = SevenRingsRecipientTests.Special.Replace("tough", status, StringComparison.Ordinal);
        var (world, game, deck, _) = BoardWithSpecial(special, false, "09036", 0);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (modifier.Length > 0)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, modifier, Amount: 1,
                Card: villain.ObjectId, Affects: villain.ObjectId));
        for (int i = 0; i < existing; i++) Statuses.Give(world, villain, status);
        var original = deck.PublicTop!;
        game.Resolve(new Decision(Mastery(game, world.Seats[1].IdentityCard).Id, [original.ObjectId], []));
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(eligible, offer.Targets!.Legal.Contains(villain.ObjectId));
        var result = game.Resolve(new Decision(offer.Id, eligible ? [villain.ObjectId] : []));
        Assert.Equal(existing + (eligible ? 1 : 0), Statuses.Count(world, villain, status));
        Assert.Equal(eligible ? 1 : 0, result.Events.OfType<CardAttached>().Count(item => item.Verb == "Give_Status"));
        Assert.Same(deck.Discard, original.Area);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3.5")]
    public void NewlyForbiddenSecondRecipientRejectsTheEntireSubsetBeforeGivingTheFirst(bool master)
    {
        // A target is invalid if another ability says it "cannot perform" the function.
        var (world, game, deck, _) = BoardWithSpecial(SevenRingsRecipientTests.Special, false, "09034", master ? 1 : 0);
        var original = deck.PublicTop!;
        var first = world.Seats[0].IdentityCard;
        var second = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(SevenRingsRecipientTests.Payment(world, game, master, original));
        var pending = game.Pending!;
        var offer = Assert.Single(pending.Affordances);
        Assert.Contains(second.ObjectId, offer.Targets!.Legal);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "cannot_be_tough", Amount: 1,
            Card: second.ObjectId, Affects: second.ObjectId));
        string before = Snapshot(world);

        Assert.ThrowsAny<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [first.ObjectId, second.ObjectId])));

        Assert.Equal(before, Snapshot(world));
        Assert.Equal(0, Statuses.Count(world, first, Statuses.Tough));
        Assert.Same(original, deck.PublicTop);
        Assert.Same(pending, game.Pending);
        var result = game.Resolve(new Decision(offer.Id, [first.ObjectId]));
        Assert.Single(result.Events.OfType<CardAttached>(), item => item.Host == first.ObjectId && item.Verb == "Give_Status");
        Assert.Equal(0, Statuses.Count(world, second, Statuses.Tough));
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
