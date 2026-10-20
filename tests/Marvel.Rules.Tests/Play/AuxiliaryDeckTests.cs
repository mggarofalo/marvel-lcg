using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>RoRS deck lifecycles differ from ordinary encounter recycling.</summary>
public sealed class AuxiliaryDeckTests
{
    [Fact]
    public void SetupUsesTheSharedSeededShuffleAndPreservesEveryPhysicalCard()
    {
        var world = Board();
        var cards = Enumerable.Range(0, 4)
            .Select(_ => world.CreateCard("weapon", world.AreaOf(DeckType.AsideDeck))).ToArray();
        var expected = cards.ToList();
        var control = new Marvel.Core.Random.EngineRandom(0);
        control.Shuffle(expected);
        var events = new List<GameEvent>();
        AuxiliaryDeck.Initialize(world, AuxiliaryDeckKind.ExperimentalWeapons, cards, "setup", events);
        Assert.Equal(expected, world.AreaOf(DeckType.ExperimentalWeaponsDeck).Cards);
        Assert.Equal(control.Generator.NextUInt32(), world.Random.Generator.NextUInt32());
        Assert.Empty(world.AreaOf(DeckType.AsideDeck).Cards);
        Assert.Equal(4, events.OfType<CardsMoved>().Count());
        Assert.Single(events.OfType<AreaReordered>());
    }

    [Fact]
    public void WeaponsAreConcealedAndDiscardIntoTheEncounterPileWithoutRecycling()
    {
        // pack:mc10:experimental-weapons-deck: "When that card is discarded,
        // it is placed in the encounter deck discard pile."
        var world = Board();
        var weapon = world.CreateCard("weapon", world.AreaOf(DeckType.AsideDeck));
        AuxiliaryDeck.Initialize(world, AuxiliaryDeckKind.ExperimentalWeapons, [weapon], "setup", []);
        Assert.False(weapon.FaceUp);
        Assert.True(DeckTypes.IsConcealedPile(weapon.Area.Type));
        Assert.Same(weapon, AuxiliaryDeck.TakeTop(world, AuxiliaryDeckKind.ExperimentalWeapons, "reveal", []));
        World.MoveToTop(weapon, world.AreaOf(DeckType.UpgradesArea));
        Discard.Card(world, weapon, "discard", []);
        Assert.Equal([weapon], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Null(AuxiliaryDeck.TakeTop(world, AuxiliaryDeckKind.ExperimentalWeapons, "reveal", []));
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Empty(world.TheCardIn(DeckType.MainSchemesArea)!.Tokens);
    }

    [Fact]
    public void LastSideSchemeDrawRecyclesOnlyItsOwnPileWithoutPenalty()
    {
        // pack:mc10:side-scheme-deck: "If the side-scheme deck is ever empty,
        // shuffle the side-scheme discard pile into the side-scheme deck."
        var world = Board();
        var scheme = world.CreateCard("side", world.AreaOf(DeckType.EncounterDeck));
        var ordinary = world.CreateCard("weapon", world.AreaOf(DeckType.EncounterDiscardPile));
        AuxiliaryDeck.Initialize(world, AuxiliaryDeckKind.SideSchemes, [scheme], "setup", []);
        var recycled = world.CreateCard("side", world.AreaOf(DeckType.SideSchemeDiscardPile));
        var events = new List<GameEvent>();
        Assert.Same(scheme, AuxiliaryDeck.TakeTop(world, AuxiliaryDeckKind.SideSchemes, "reveal", events));
        Assert.Equal([recycled], world.AreaOf(DeckType.SideSchemeDeck).Cards);
        Assert.Equal([ordinary], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.False(recycled.FaceUp);
        Assert.Contains(events, happened => happened.Verb == "Reset");
        Assert.DoesNotContain(events, happened => happened.Verb == "Accelerate");
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void AnEmptyPairWaitsUntilAnySideSchemeIsDiscarded()
    {
        // "When a side-scheme is defeated or otherwise discarded, place it in
        // the side-scheme discard pile." This also covers a later nemesis scheme.
        var world = Board();
        AuxiliaryDeck.Initialize(world, AuxiliaryDeckKind.SideSchemes, [], "setup", []);
        Assert.Null(AuxiliaryDeck.TakeTop(world, AuxiliaryDeckKind.SideSchemes, "reveal", []));
        Assert.Equal(Outcome.Unfinished, world.Result);
        var scheme = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        Discard.Card(world, scheme, "defeat", []);
        Assert.Equal([scheme], world.AreaOf(DeckType.SideSchemeDeck).Cards);
        Assert.Empty(world.AreaOf(DeckType.SideSchemeDiscardPile).Cards);
        Assert.Empty(world.TheCardIn(DeckType.MainSchemesArea)!.Tokens);
    }

    [Fact]
    public void WithoutTheSeparateDeckSideSchemesUseTheOrdinaryEncounterDiscard()
    {
        var world = Board();
        var scheme = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        Discard.Card(world, scheme, "defeat", []);
        Assert.Equal([scheme], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.DoesNotContain(world.Areas, area => area.Type == DeckType.SideSchemeDeck);
    }

    [Fact]
    public void InvalidSetupIsRejectedBeforeMovingCardsOrCreatingADeck()
    {
        var world = Board();
        var source = world.AreaOf(DeckType.AsideDeck);
        var card = world.CreateCard("weapon", source);
        Assert.Throws<ArgumentException>(() => AuxiliaryDeck.Initialize(
            world, AuxiliaryDeckKind.ExperimentalWeapons, [card, card], "setup", []));
        Assert.Equal([card], source.Cards);
        Assert.DoesNotContain(world.Areas, area => area.Type == DeckType.ExperimentalWeaponsDeck);
        AuxiliaryDeck.Initialize(world, AuxiliaryDeckKind.ExperimentalWeapons, [card], "setup", []);
        Assert.Throws<InvalidOperationException>(() => AuxiliaryDeck.Initialize(
            world, AuxiliaryDeckKind.ExperimentalWeapons, [], "setup", []));
        Assert.Equal([card], world.AreaOf(DeckType.ExperimentalWeaponsDeck).Cards);
    }

    private static World Board()
    {
        var world = new World(new Printed(), 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        world.CreateCard("main", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "main" => CardKind.MainScheme,
            "side" => CardKind.EncounterSideScheme,
            _ => CardKind.Attachment,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
