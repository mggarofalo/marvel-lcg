using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Physical encounter ownership required by pack:mc03:new-rules.</summary>
public sealed class EncounterTopologyTests
{
    [Fact]
    public void ExtremeStagePromotionRetainsDeckOwnershipAndMovesRegisteredActiveState()
    {
        // pack:mc03:adjustable-difficulty: version B enters when version A is defeated.
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        var versionB = world.CreateCard("villain", world.AreaOf(DeckType.VillainDeck));
        var staged = new EncounterDeckGroup(first.Villain, first.Scheme, first.Deck,
            first.Discard, first.Members, [first.Villain, versionB]);
        var topology = new EncounterTopology(world, [staged, second], first.Villain);
        world.BindEncounterTopology(topology);
        World.MoveToTop(first.Villain, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(versionB, world.AreaOf(DeckType.VillainArea));

        Assert.Same(versionB, EncounterRouting.Villain(world));
        Assert.Equal(0, topology.Fields(first.Villain).Single().Value);
        Assert.Equal(1, topology.Fields(versionB).Single().Value);
        Assert.Equal(first.Villain.ObjectId, topology.Fields(first.Members[0]).Single().Value);
        Assert.Same(first.Deck, EncounterRouting.Deck(world));
    }

    [Fact]
    public void BoostUsesActiveDeckEvenForADifferentActivatingVillain()
    {
        // pack:mc03:new-rules: "When the villain is dealt a boost card,
        // it is dealt from the active villain's deck."
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        world.BindEncounterTopology(new EncounterTopology(world, [first, second], second.Villain));
        var events = new List<Marvel.Rules.Events.GameEvent>();

        Attack.GiveAdditionalBoostCard(world, first.Villain, "test", events);

        var moved = Assert.Single(events.OfType<Marvel.Rules.Events.CardsMoved>());
        Assert.Equal(Places.Reference(second.Deck), moved.From);
        Assert.Equal(first.Villain.ObjectId, second.Members[1].Area.Host);
        Assert.Equal(2, first.Deck.Cards.Count);
        Assert.Single(second.Deck.Cards);
    }

    [Fact]
    public void ResetShufflesOnlyTheActiveVillainsDiscard()
    {
        // pack:mc03:new-rules: each villain's empty deck reshuffles its own discard.
        var world = new World(new Printed(), 1);
        world.CreateCard("main", world.AreaOf(DeckType.MainSchemesArea));
        var first = Group(world);
        var second = Group(world);
        world.BindEncounterTopology(new EncounterTopology(world, [first, second], second.Villain));
        foreach (var card in second.Members) World.MoveToTop(card, second.Discard);

        Assert.True(EncounterDeck.Reset(world, "test", []));

        Assert.Equal(first.Members, first.Deck.Cards);
        Assert.Equal(second.Members.Select(card => card.ObjectId).Order(),
            second.Deck.Cards.Select(card => card.ObjectId).Order());
        Assert.Empty(second.Discard.Cards);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Fact]
    public void ActiveCounterChangesDrawPileButDoesNotChangeDiscardOwnership()
    {
        // pack:mc03:new-rules: dealt encounter cards come from the active villain's deck.
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        var topology = new EncounterTopology(world, [first, second], first.Villain);
        world.BindEncounterTopology(topology);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        topology.SelectActive(second.Villain, "test", events);

        var drawn = Deal.EncounterCard(world, 0, "test", events);
        Assert.Same(second.Members[1], drawn);
        Assert.Equal(2, first.Deck.Cards.Count);
        Assert.Single(second.Deck.Cards);
        Assert.Same(second.Villain, EncounterRouting.Villain(world));
        topology.SelectActive(first.Villain, "test", events);
        Discard.Card(world, drawn!, "test", events);
        Assert.Equal([drawn!], second.Discard.Cards);
        Assert.Empty(first.Discard.Cards);
    }

    [Fact]
    public void DigestRegistersZeroActiveCountersAndPersistentOwnership()
    {
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        var topology = new EncounterTopology(world, [first, second], first.Villain);
        world.BindEncounterTopology(topology);
        Assert.Equal(1, topology.Fields(first.Villain).Single().Value);
        Assert.Equal(0, topology.Fields(second.Villain).Single().Value);
        Assert.Contains("\"active_villain\":0", world.Digest().Canonical());
        Assert.Contains("\"encounter_villain\":" + second.Villain.ObjectId,
            world.Digest().Canonical());
        World.MoveToTop(second.Members[0], world.AreaOf(DeckType.RemovedArea));
        Assert.Equal(second.Villain.ObjectId, topology.Fields(second.Members[0]).Single().Value);
        Assert.Empty(topology.Fields(first.Scheme));
    }

    [Fact]
    public void RemovedVillainCannotReceiveActiveCounter()
    {
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        var topology = new EncounterTopology(world, [first, second], first.Villain);
        World.MoveToTop(second.Villain, world.AreaOf(DeckType.RemovedArea));
        Assert.Throws<ArgumentException>(() => topology.SelectActive(second.Villain, "test", []));
        Assert.Same(first, topology.Active);
    }

    [Fact]
    public void DiscardReturnsToOriginalDeckAfterEnteringAnotherArea()
    {
        // "When an encounter card leaves play, it is placed in the discard
        // pile of its corresponding encounter deck." Current location is not ownership.
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        world.BindEncounterTopology(new EncounterTopology(world, [first, second], first.Villain));
        var card = second.Members[0];
        World.MoveToTop(card, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Discard.Card(world, card, "test", []);

        Assert.Equal([card], second.Discard.Cards);
        Assert.Empty(first.Discard.Cards);
        Assert.Same(second, world.EncounterTopology!.ForCard(card));
    }

    [Fact]
    public void OwnershipSurvivesVillainRemoval()
    {
        // "Any encounter cards from that villain's deck that are in play remain in play."
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        world.BindEncounterTopology(new EncounterTopology(world, [first, second], first.Villain));
        World.MoveToTop(second.Villain, world.AreaOf(DeckType.RemovedArea));
        Discard.Card(world, second.Members[0], "test", []);
        Assert.Same(second.Discard, second.Members[0].Area);
        Assert.Empty(first.Discard.Cards);
    }

    [Fact]
    public void DuplicateMembershipIsRejected()
    {
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        var invalid = new EncounterDeckGroup(second.Villain, second.Scheme,
            second.Deck, second.Discard, [.. second.Members, first.Members[0]]);
        Assert.Throws<ArgumentException>(() => new EncounterTopology(world, [first, invalid], first.Villain));
    }

    [Fact]
    public void MissingPileMembershipIsRejected()
    {
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        var invalid = new EncounterDeckGroup(second.Villain, second.Scheme,
            second.Deck, second.Discard, []);
        Assert.Throws<ArgumentException>(() => new EncounterTopology(world, [first, invalid], first.Villain));
    }

    [Fact]
    public void ForeignWorldWithMatchingObjectIdsIsRejected()
    {
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var foreign = Group(new World(new Printed(), 1));
        Assert.Throws<ArgumentException>(() => new EncounterTopology(world, [first, foreign], first.Villain));
    }

    [Fact]
    public void UnmappedEncounterCardFailsInsteadOfUsingFirstDiscard()
    {
        var world = new World(new Printed(), 1);
        var first = Group(world);
        var second = Group(world);
        world.BindEncounterTopology(new EncounterTopology(world, [first, second], first.Villain));
        var unknown = world.CreateCard("minion", world.AreaOf(DeckType.RevealingArea));
        Assert.Throws<RulesNotImplementedException>(() =>
            Discard.Card(world, unknown, "test", []));
        Assert.Empty(first.Discard.Cards);
        Assert.Empty(second.Discard.Cards);
    }

    private static EncounterDeckGroup Group(World world)
    {
        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        var deck = world.CreateArea(DeckType.EncounterDeck);
        var discard = world.CreateArea(DeckType.EncounterDiscardPile);
        var card = world.CreateCard("minion", deck);
        var next = world.CreateCard("minion", deck);
        return new EncounterDeckGroup(villain, scheme, deck, discard, [card, next]);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "villain" => CardKind.EncounterVillain,
            "side" => CardKind.EncounterSideScheme,
            "main" => CardKind.MainScheme,
            _ => CardKind.Minion,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
