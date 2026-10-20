namespace Marvel.Rules.State;

/// <summary>The physical components belonging to one villain's encounter deck.</summary>
/// <remarks>Engine-chosen setup contract; membership follows physical cards across zones.</remarks>
public sealed class EncounterDeckGroup
{
    /// <summary>Captures an ordered membership snapshot supplied by scenario setup.</summary>
    public EncounterDeckGroup(Card villain, Card scheme, Area deck, Area discard,
        IReadOnlyList<Card> members, IReadOnlyList<Card>? stages = null)
    {
        Villain = villain;
        Scheme = scheme;
        Deck = deck;
        Discard = discard;
        Members = Array.AsReadOnly(members.ToArray());
        Stages = Array.AsReadOnly((stages ?? [villain]).ToArray());
    }

    /// <summary>The villain whose printed set supplies this deck.</summary>
    public Card Villain { get; }
    /// <summary>Physical stages in progression order; the first also identifies immutable deck ownership.</summary>
    public IReadOnlyList<Card> Stages { get; }
    /// <summary>The currently living stage, or null after the group's final defeat.</summary>
    public Card? CurrentVillain => Stages.SingleOrDefault(card => card.Area.Type == DeckType.VillainArea);
    /// <summary>The side scheme paired with the villain.</summary>
    public Card Scheme { get; }
    /// <summary>The independently shuffled draw pile.</summary>
    public Area Deck { get; }
    /// <summary>The corresponding discard pile.</summary>
    public Area Discard { get; }
    /// <summary>Physical encounter cards, in deterministic setup order.</summary>
    public IReadOnlyList<Card> Members { get; }
}
