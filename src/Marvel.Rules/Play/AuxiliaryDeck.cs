using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The separate decks defined by the Rise of Red Skull rulebook.</summary>
public static class AuxiliaryDeck
{
    /// <summary>Creates and shuffles a scenario deck at its printed setup instruction.</summary>
    public static void Initialize(
        World world, AuxiliaryDeckKind kind, IReadOnlyList<Card> cards,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(events);
        var type = Type(kind);
        if (world.Areas.Any(area => area.Type == type))
            throw new InvalidOperationException("the auxiliary deck is already initialized");
        ValidateCards(world, cards);
        var deck = world.AreaOf(type);
        if (kind == AuxiliaryDeckKind.SideSchemes)
            world.AreaOf(DeckType.SideSchemeDiscardPile);
        foreach (var card in cards)
            Move(card, deck, trigger, "Setup", events);
        Shuffle(world, deck, trigger, events);
    }

    /// <summary>Takes one card for the ordinary reveal pipeline, without an encounter penalty.</summary>
    public static Card? TakeTop(
        World world, AuxiliaryDeckKind kind, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        var deck = world.Areas.SingleOrDefault(area => area.Type == Type(kind))
            ?? throw new InvalidOperationException("the auxiliary deck is not initialized");
        if (kind == AuxiliaryDeckKind.SideSchemes) ResetSideSchemes(world, trigger, events);
        var card = deck.TakeTop();
        if (kind == AuxiliaryDeckKind.SideSchemes) ResetSideSchemes(world, trigger, events);
        return card;
    }

    /// <summary>Resets an empty side-scheme deck whenever its discard pile has cards.</summary>
    public static bool ResetSideSchemes(World world, string trigger, List<GameEvent> events)
    {
        // pack:mc10:side-scheme-deck: "If the side-scheme deck is ever empty,
        // shuffle the side-scheme discard pile into the side-scheme deck.
        // There is no penalty for doing this."
        var deck = world.Areas.SingleOrDefault(area => area.Type == DeckType.SideSchemeDeck);
        if (deck is null || deck.Cards.Count > 0) return false;
        var pile = world.AreaOf(DeckType.SideSchemeDiscardPile);
        if (pile.Cards.Count == 0) return false;
        foreach (var card in pile.Cards.ToArray())
            Move(card, deck, trigger, "Reset", events);
        Shuffle(world, deck, trigger, events);
        return true;
    }

    /// <summary>Whether the scenario routes this card to its separate side-scheme discard.</summary>
    public static bool UsesSideSchemeDiscard(World world, Card card) =>
        card.Owner < 0 && world.Facts.Kind(card.FaceId) == CardKind.EncounterSideScheme
        && world.Areas.Any(area => area.Type == DeckType.SideSchemeDeck);

    private static DeckType Type(AuxiliaryDeckKind kind) => kind switch
    {
        AuxiliaryDeckKind.ExperimentalWeapons => DeckType.ExperimentalWeaponsDeck,
        AuxiliaryDeckKind.SideSchemes => DeckType.SideSchemeDeck,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static void ValidateCards(World world, IReadOnlyList<Card> cards)
    {
        if (cards.Distinct().Count() != cards.Count)
            throw new ArgumentException("auxiliary deck contains duplicate physical cards", nameof(cards));
        foreach (var card in cards)
        {
            if (!world.Cards.Contains(card) || card.Owner >= 0
                || card.Area.Type is not (DeckType.AsideDeck or DeckType.EncounterDeck))
                throw new ArgumentException("auxiliary setup requires this world's set-aside or encounter cards", nameof(cards));
        }
    }

    private static void Move(Card card, Area deck, string trigger, string verb, List<GameEvent> events)
    {
        var from = card.Area;
        World.MoveToTop(card, deck);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(deck),
            [new Landing(card.ObjectId, deck.Cards.Count - 1)])
        {
            Trigger = trigger,
            Verb = verb,
        });
    }

    private static void Shuffle(World world, Area deck, string trigger, List<GameEvent> events)
    {
        world.Shuffle(deck);
        events.Add(new AreaReordered(Places.Reference(deck), [.. deck.Cards.Select(card => card.ObjectId)])
        {
            Trigger = trigger,
            Verb = "Shuffle",
        });
    }
}
