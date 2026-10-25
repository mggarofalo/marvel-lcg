using Marvel.Rules.Play;
using Marvel.Rules.Events;

namespace Marvel.Rules.State;

/// <summary>Validated physical ownership for a scenario with separate encounter decks.</summary>
/// <remarks>
/// pack:mc03:new-rules: encounter cards return to their corresponding discard pile.
/// Membership is immutable and independent of the active villain or the card's current zone.
/// </remarks>
public sealed class EncounterTopology
{
    private readonly Dictionary<int, EncounterDeckGroup> membership = [];

    /// <summary>Validates a setup-defined topology without changing any cards or RNG state.</summary>
    public EncounterTopology(World world, IReadOnlyList<EncounterDeckGroup> groups, Card active)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count < 2)
            throw new ArgumentException("separate encounter decks require at least two groups", nameof(groups));
        World = world;
        Groups = Array.AsReadOnly(groups.ToArray());
        var villains = new HashSet<int>();
        var schemes = new HashSet<int>();
        var areas = new HashSet<int>();
        foreach (var group in Groups)
        {
            RequirePrincipals(group, villains, schemes);
            RequireArea(group.Deck, DeckType.EncounterDeck, areas);
            RequireArea(group.Discard, DeckType.EncounterDiscardPile, areas);
            AddMembers(group);
        }
        Active = RequireLivingVillain(active);
    }

    /// <summary>The single game whose physical components were validated.</summary>
    public World World { get; }
    /// <summary>Groups in authored setup order, never dictionary iteration order.</summary>
    public IReadOnlyList<EncounterDeckGroup> Groups { get; }
    /// <summary>The sole villain holding the active counter.</summary>
    public EncounterDeckGroup Active { get; private set; }

    /// <summary>Moves the single active counter to a living villain chosen by the rules procedure.</summary>
    public void SelectActive(Card villain, string trigger, List<GameEvent> events)
    {
        var next = RequireLivingVillain(villain);
        if (ReferenceEquals(next, Active)) return;
        var prior = Active;
        Active = next;
        if (prior.CurrentVillain is { } previous)
            events.Add(new FieldSet(previous.ObjectId, "active_villain", 1, 0)
                { Trigger = trigger, Verb = "Remove" });
        events.Add(new FieldSet(villain.ObjectId, "active_villain", 0, 1)
            { Trigger = trigger, Verb = "Place" });
    }

    /// <summary>Registered state, including zero and immutable ownership outside play.</summary>
    public IEnumerable<KeyValuePair<string, long>> Fields(Card card)
    {
        if (membership.TryGetValue(card.ObjectId, out var owner))
            yield return new("encounter_villain", owner.Villain.ObjectId);
        if (Groups.Any(group => group.Stages.Contains(card)))
            yield return new("active_villain", ReferenceEquals(Active.CurrentVillain, card) ? 1 : 0);
    }

    private EncounterDeckGroup RequireLivingVillain(Card villain)
    {
        RequireCard(villain);
        return Groups.SingleOrDefault(group => group.Stages.Contains(villain)
            && villain.Area.Type == DeckType.VillainArea)
            ?? throw new ArgumentException("the active counter requires a participating villain in play");
    }

    private void RequirePrincipals(EncounterDeckGroup group, HashSet<int> villains, HashSet<int> schemes)
    {
        RequireCard(group.Villain);
        RequireCard(group.Scheme);
        if (World.Facts.Kind(group.Scheme.FaceId) != CardKind.EncounterSideScheme
            || !schemes.Add(group.Scheme.ObjectId) || group.Stages.Count == 0
            || !ReferenceEquals(group.Stages[0], group.Villain))
            throw new ArgumentException("each group requires a distinct villain and side scheme");
        foreach (var stage in group.Stages)
        {
            RequireCard(stage);
            if (World.Facts.Kind(stage.FaceId) != CardKind.EncounterVillain || !villains.Add(stage.ObjectId))
                throw new ArgumentException("villain stages must be distinct physical villain cards");
        }
        if (group.CurrentVillain is null)
            throw new ArgumentException("each encounter group requires one living villain at setup");
    }

    private void AddMembers(EncounterDeckGroup group)
    {
        foreach (var card in group.Members)
        {
            RequireCard(card);
            if (card.Owner != global::Marvel.Rules.State.World.Scenario
                || Groups.Any(other => other.Stages.Contains(card) || ReferenceEquals(other.Scheme, card))
                || !membership.TryAdd(card.ObjectId, group))
                throw new ArgumentException("encounter membership must contain distinct scenario cards");
        }
        if (group.Deck.Cards.Concat(group.Discard.Cards).Any(card => !group.Members.Contains(card)))
            throw new ArgumentException("every card in a paired pile must have matching membership");
    }

    /// <summary>Finds physical origin even after dealing, playing or discarding a card.</summary>
    public EncounterDeckGroup ForCard(Card card)
    {
        RequireCard(card);
        return membership.TryGetValue(card.ObjectId, out var group) ? group
            : throw new RulesNotImplementedException(
                $"encounter card {card.ObjectId} has no configured encounter-deck membership");
    }

    private void RequireCard(Card card)
    {
        if (card.ObjectId < 0 || card.ObjectId >= World.Cards.Count
            || !ReferenceEquals(World.Cards[card.ObjectId], card))
            throw new ArgumentException("topology card belongs to a different world");
    }

    private void RequireArea(Area area, DeckType type, HashSet<int> areas)
    {
        if (area.Id < 0 || area.Id >= World.Areas.Count
            || !ReferenceEquals(World.Areas[area.Id], area) || area.Type != type
            || area.PlayArea != PlayArea.Villains || area.CardOwner != global::Marvel.Rules.State.World.Scenario
            || area.Host != -1 || !areas.Add(area.Id))
            throw new ArgumentException("encounter piles must be distinct unhosted scenario areas");
    }
}
