using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local printed Vapors through the nonpublic Invocation lifecycle.</summary>
public sealed class VaporsStatusReplacementTests
{
    private const string Special = """
        {"seq":[{"chooseCard":{"from":{"query":"statusCards"},"effect":{"choose":{
          "descriptions":["Replace with tough","Replace with stunned","Replace with confused"],
          "options":[{"replaceStatus":{"card":"chosen","status":"tough"}},
          {"replaceStatus":{"card":"chosen","status":"stunned"}},
          {"replaceStatus":{"card":"chosen","status":"confused"}}]}}}},
          {"discardInvocation":"this"}]}
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:status-cards.1")]
    public void NonFirstStrangeReplacesExactStatusOnSameCharacterThroughBothArrows(bool master)
    {
        // "A character cannot have more than one status card of each type at a time."
        var (world, game, deck, _) = BoardWithSpecial(Special, false, "09035", master ? 1 : 0);
        var host = world.Seats[0].IdentityCard;
        var old = Statuses.Give(world, host, Statuses.Stunned);
        var other = Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Tough);
        var original = deck.PublicTop!;
        Begin(world, game, original, master);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Contains(game.Pending.Affordances, offer => offer.AnchorId == old.ObjectId);
        var bad = Decision.Take(int.MaxValue);
        string snapshot = Snapshot(world);
        Assert.ThrowsAny<Exception>(() => game.Resolve(bad));
        Assert.Equal(snapshot, Snapshot(world));
        game.Resolve(Decision.Take(game.Pending.Affordances.Single(offer => offer.AnchorId == old.ObjectId).Id));
        Assert.Equal(1, game.Pending!.Player);
        Assert.DoesNotContain(game.Pending.Affordances, offer => offer.Description == "Replace with stunned");
        snapshot = Snapshot(world);
        Assert.ThrowsAny<Exception>(() => game.Resolve(bad));
        Assert.Equal(snapshot, Snapshot(world));
        var tough = game.Pending.Affordances.Single(offer => offer.Description == "Replace with tough");
        var result = game.Resolve(Decision.Take(tough.Id));
        Assert.Equal(DeckType.RemovedArea, old.Area.Type);
        Assert.Equal(1, Statuses.Count(world, host, Statuses.Tough));
        Assert.Equal(0, Statuses.Count(world, host, Statuses.Stunned));
        Assert.Equal(DeckType.StatusArea, other.Area.Type);
        Assert.Contains(result.Events.OfType<CardAttached>(), item => item.Host == host.ObjectId && item.Verb == "Replace_Status");
        Assert.DoesNotContain(result.Events, item => item.Verb == "Give_Status");
        Assert.Equal(master ? DeckType.AdditionalDeck : DeckType.AdditionalDiscardPile, original.Area.Type);
        Assert.Equal(master, ReferenceEquals(deck.PublicTop, original));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("ruling:8883bc6b0af1c9b8")]
    public void NoReplaceableStatusStillDiscardsAndReveals(bool master, bool blocked)
    {
        // "You can trigger the Special on Vapors ... even when there isn't a status card in play."
        var (world, game, deck, _) = BoardWithSpecial(Special, false, "09035", master ? 1 : 0);
        if (blocked)
        {
            var host = world.TheCardIn(DeckType.VillainArea)!;
            world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(Marvel.Rules.Timing.EffectSource.LastingEffect, "stalwart", Amount: 1, Card: host.ObjectId, Affects: host.ObjectId));
            Statuses.Give(world, host, Statuses.Tough);
        }
        var original = deck.PublicTop!;
        var next = deck.Deck.Cards[^2];
        var result = Begin(world, game, original, master);
        Assert.True(game.IsRootPrompt);
        Assert.Equal(master ? original : next, deck.PublicTop);
        Assert.Contains(result.Events.OfType<CardsMoved>(), item => item.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:status-cards.1")]
    [Rule("rr:stalwart.1")]
    public void BlockedCardsAndDuplicateDestinationsAreFiltered(bool master)
    {
        // "This character cannot have confused or stunned status cards."
        var (world, game, deck, _) = BoardWithSpecial(Special, false, "09035", master ? 1 : 0);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(
            Marvel.Rules.Timing.EffectSource.LastingEffect, "stalwart", Amount: 1,
            Card: villain.ObjectId, Affects: villain.ObjectId));
        var blocked = Statuses.Give(world, villain, Statuses.Tough);
        var host = world.Seats[0].IdentityCard;
        var old = Statuses.Give(world, host, Statuses.Stunned);
        Statuses.Give(world, host, Statuses.Tough);
        Begin(world, game, deck.PublicTop!, master);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == blocked.ObjectId);
        var selected = game.Pending.Affordances.Single(offer => offer.AnchorId == old.ObjectId);
        game.Resolve(Decision.Take(selected.Id));
        Assert.Equal("Replace with confused", Assert.Single(game.Pending!.Affordances).Description);
        string before = Snapshot(world);
        Assert.ThrowsAny<Exception>(() => game.Resolve(Decision.Take(selected.Id)));
        Assert.Equal(before, Snapshot(world));
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.True(game.IsRootPrompt);
        Assert.Equal(1, Statuses.Count(world, host, Statuses.Confused));
        Assert.Equal(DeckType.StatusArea, blocked.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Resolution Begin(World world, Game game, Card original, bool master)
    {
        var hero = world.Seats[1].IdentityCard;
        var offer = master ? game.Pending!.Affordances.Single(item => item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId)
            : Mastery(game, hero);
        int[] sources = master ? [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId] : [];
        var allocations = master ? ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources) : null;
        return game.Resolve(new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations));
    }
}
