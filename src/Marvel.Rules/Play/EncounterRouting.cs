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

    /// <summary>The villain this card's text means by “the villain”.</summary>
    /// <remarks>
    /// pack:mc11:game-areas: "Cards and components in one game area cannot affect
    /// another game area". While the table is split each game area has its own
    /// Kang, so the villain is the one in the referring card's game area. A card
    /// in no game area (Kang's central 2B) reaches every Kang, and which one it
    /// means is not settled by any rule, so that raises.
    /// </remarks>
    public static Card? Villain(World world, Card source)
    {
        if (world.EncounterTopology is { } topology) return topology.Active.CurrentVillain;
        if (!SeparateGameAreas.Active(world)) return world.TheCardIn(DeckType.VillainArea);
        var candidates = world.Areas.Where(area => area.Type == DeckType.VillainArea)
            .SelectMany(area => area.Cards).Where(card => Places.CanAffect(world, source, card)).ToList();
        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw new RulesNotImplementedException(
                $"'{source.FaceId}' refers to the villain from outside every Kang's game area"),
        };
    }

    /// <summary>The villain that can activate against this player in their game area.</summary>
    public static Card? VillainForPlayer(World world, int player)
    {
        // pack:mc03:new-rules selects one active villain in the shared area.
        if (world.EncounterTopology is not null) return Villain(world);

        // pack:mc11:game-areas: "Players cannot attack or defend enemies in
        // other game areas" and cards cannot affect another game area.
        var identity = world.Seats[player].IdentityCard;
        var candidates = world.Cards.Where(card => card.Area.Type == DeckType.VillainArea
            && Places.CanAffect(world, identity, card)).ToList();
        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw new RulesNotImplementedException(
                "multiple villains can activate in this player's game area"),
        };
    }

    /// <summary>The current encounter draw pile; ordinary games retain their existing area lookup.</summary>
    public static Area Deck(World world) =>
        world.EncounterTopology?.Active.Deck ?? world.AreaOf(DeckType.EncounterDeck);

    /// <summary>The discard paired with the current draw pile.</summary>
    public static Area Discard(World world) =>
        world.EncounterTopology?.Active.Discard ?? world.AreaOf(DeckType.EncounterDiscardPile);
}
