using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingSelectedDeckTests : CoreCardTestBase
{
    [Rule("rr:encounter-deck.1")]
    [Fact]
    public void LastSelectedCardReshufflesOnlyItsPairedDiscardBeforeResolution()
    {
        // "the encounter discard pile is immediately shuffled to create a new encounter deck".
        // FFG ruling23fba0d298997c93 applies this order to Buddy System's selected deck.
        var world = Hero("01001a");
        var main = world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var active = Group(world, "07002", "07004");
        var selected = Group(world, "07017", "07019");
        world.BindEncounterTopology(new EncounterTopology(world, [active, selected], active.Villain));
        foreach (var card in selected.Members.Take(2)) World.MoveToTop(card, selected.Discard);
        var top = selected.Members[2];
        var activeOrder = active.Deck.Cards.ToArray();
        long words = world.Random.Generator.WordsConsumed;
        List<GameEvent> events = [];

        var drawn = EncounterDeck.TakeTopFromVillain(world, selected.Villain, "Buddy System", events);

        Assert.Same(top, drawn);
        Assert.Equal(activeOrder, active.Deck.Cards);
        Assert.Empty(active.Discard.Cards);
        Assert.Empty(selected.Discard.Cards);
        Assert.Equal(selected.Members.Take(2).Select(card => card.ObjectId).Order(),
            selected.Deck.Cards.Select(card => card.ObjectId).Order());
        Assert.DoesNotContain(top, selected.Deck.Cards);
        Assert.Same(active, world.EncounterTopology!.Active);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.Equal(1, main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Collection(events,
            item =>
            {
                var moved = Assert.IsType<CardsMoved>(item);
                Assert.Equal("Reset", moved.Verb);
                Assert.Equal(Places.Reference(selected.Discard), moved.From);
                Assert.Equal(Places.Reference(selected.Deck), moved.To);
            },
            item => Assert.IsType<AreaReordered>(item),
            item => Assert.IsType<FieldSet>(item));
    }

    [Fact]
    public void InvalidPhysicalVillainCannotFallBackToTheActiveDeck()
    {
        var world = Hero("01001a");
        var active = Group(world, "07002", "07004");
        var selected = Group(world, "07017", "07019");
        world.BindEncounterTopology(new EncounterTopology(world, [active, selected], active.Villain));
        var foreign = world.CreateCard(selected.Villain.FaceId, world.AreaOf(DeckType.VillainArea));
        World.MoveToTop(selected.Villain, world.AreaOf(DeckType.RemovedArea));
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        List<GameEvent> events = [];

        foreach (var target in new[] { selected.Villain, foreign, selected.Scheme })
            Assert.Throws<RulesNotImplementedException>(() =>
                EncounterDeck.TakeTopFromVillain(world, target, "test", events));

        Assert.Empty(events);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
        Assert.Equal(3, active.Deck.Cards.Count);
        Assert.Equal(3, selected.Deck.Cards.Count);
    }

    [Fact]
    public void OrdinaryNonemptyDrawDoesNotAllocateADiscardArea()
    {
        // Engine identity contract: a draw that does not empty the deck does not
        // need a discard area, so later area allocation retains its exact IDs.
        var world = new World(Cards, 41);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01110", deck);
        var top = world.CreateCard("01111", deck);
        int count = world.Areas.Count;

        Assert.Same(top, EncounterDeck.TakeTop(world, "test", []));

        Assert.Equal(count, world.Areas.Count);
        Assert.DoesNotContain(world.Areas, area => area.Type == DeckType.EncounterDiscardPile);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.Single(deck.Cards);
    }

    private static EncounterDeckGroup Group(World world, string villain, string scheme)
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        Card[] members = [world.CreateCard("07037", deck), world.CreateCard("07037", deck),
            world.CreateCard("07027", deck)];
        return new(world.CreateCard(villain, world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)), deck,
            world.CreateArea(DeckType.EncounterDiscardPile), members);
    }
}
