using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Resolves the active villain's encounter piles without changing physical ownership.</summary>
public static class EncounterRouting
{
    /// <summary>Finds the exact living physical villain's immutable deck group.</summary>
    public static EncounterDeckGroup CorrespondingGroup(World world, Card villain) =>
        world.EncounterTopology?.Groups.SingleOrDefault(group =>
            ReferenceEquals(group.CurrentVillain, villain))
        ?? throw new RulesNotImplementedException(
            $"'{villain.FaceId}' requires its living corresponding encounter deck");

    /// <summary>pack:mc03:new-rules: “the villain” refers to the active villain.</summary>
    public static Card? Villain(World world) =>
        world.EncounterTopology is { } topology
            ? topology.Active.CurrentVillain : world.TheCardIn(DeckType.VillainArea);

    /// <summary>The current encounter draw pile; ordinary games retain their existing area lookup.</summary>
    public static Area Deck(World world) =>
        world.EncounterTopology?.Active.Deck ?? world.AreaOf(DeckType.EncounterDeck);

    /// <summary>The discard paired with the current draw pile.</summary>
    public static Area Discard(World world) =>
        world.EncounterTopology?.Active.Discard ?? world.AreaOf(DeckType.EncounterDiscardPile);
}
