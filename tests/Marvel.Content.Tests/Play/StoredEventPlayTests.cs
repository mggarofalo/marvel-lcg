using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic event effects exercise the complete storage, timing and payment path.</summary>
public sealed class StoredEventPlayTests
{
    [Theory]
    [InlineData("Action")]
    [InlineData("Interrupt")]
    public void StoredEventsUseNormalCostsAndLeaveTheirActualStorageZone(string timing)
    {
        var runner = RunnerFor(timing);
        Card? arrow = null;
        Card? payment = null;
        Card? host = null;
        var (_, world) = Playing(board =>
        {
            Hand(board, "01003", 0);
            host = InPlay(board, "01006");
            arrow = board.CreateCard("04005", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            BeneathCards.Place(board, arrow, host, true, "synthetic", []);
        }, hero: true, abilities: runner);
        var occurrence = new Occurrence(1, ["WhenDamageWouldBeDealt"], Player: 0,
            Target: world.Seats[0].IdentityCard.ObjectId);
        string before = world.Digest().Canonical();
        int held = world.Seats[0].Hand.Cards.Count;
        var rng = world.Random.Generator.GetState();
        var pending = Assert.Single(timing == "Action" ? runner.Actions(world, 0)
            : runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == arrow!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[0], runner),
            generator => generator.Effect == arrow!.ObjectId);
        var events = timing == "Action"
            ? runner.ExecuteAction(world, new PlayerAction(pending, [payment!.ObjectId], [])).Events
            : runner.ResolveWindow(world, occurrence, pending, new(arrow!.ObjectId, [], [payment!.ObjectId])).Events;
        Assert.Equal(DeckType.DiscardPile, arrow!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment!.Area.Type);
        Assert.Empty(BeneathCards.Of(world, host!));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Contains(events.OfType<CardsMoved>(), moved =>
            moved.From.Zone == nameof(DeckType.BeneathCardsArea) && moved.Cards.Any(card => card.Card == arrow.ObjectId));
        Assert.DoesNotContain(events.OfType<CardsMoved>(), moved =>
            moved.To.Zone == nameof(DeckType.HandsArea) && moved.Cards.Any(card => card.Card == arrow.ObjectId));
    }

    [Fact]
    public void HostDepartureInvalidatesAnOfferedStoredEventBeforePayment()
    {
        var runner = RunnerFor("Action");
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        Hand(world, "01088", 1);
        var host = InPlay(world, "01006");
        var arrow = world.CreateCard("04005", world.Seats[0].Hand);
        BeneathCards.Place(world, arrow, host, true, "synthetic", []);
        var pending = Assert.Single(runner.Actions(world, 0), action => action.Card == arrow.ObjectId);
        var payment = Assert.Single(world.Seats[0].Hand.Cards);
        Discard.Card(world, host, "synthetic", []);
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == arrow.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new(pending, [payment.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(world.Seats[0].Hand, payment.Area);
    }

    private static AbilityRunner RunnerFor(string timing) => new(AbilityCatalog.Parse($$$$"""
        {"cards":[
          {"card":"01006","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
            "effect":{"allowPlayBeneath":{"kind":"Event","trait":"ARROW"}}}]},
          {"card":"04005","abilities":[{"trigger":{"timing":"{{{{timing}}}}","event":"WhenDamageWouldBeDealt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}}]}
        ]}
        """));
}
