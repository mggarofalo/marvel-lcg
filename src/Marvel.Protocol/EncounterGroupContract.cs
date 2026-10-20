using Marvel.View;

namespace Marvel.Protocol;

/// <summary>Checks referential integrity of published encounter relationships without deciding gameplay.</summary>
public static class EncounterGroupContract
{
    /// <summary>Rejects dangling, concealed, duplicated or contradictory group metadata.</summary>
    public static bool IsValid(WorldDescriptor world)
    {
        if (world.EncounterGroups is null) return false;
        if (world.EncounterGroups.Count == 0) return true;
        if (!HasAreas(world))
            return false;
        var readable = world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card is not null && card.Id is not null && card.Face is not null)
            .Select(card => card.Id!.Value).ToHashSet();
        var groups = new HashSet<int>();
        var stages = new HashSet<int>();
        var piles = new HashSet<int>();
        foreach (var group in world.EncounterGroups)
            if (!ValidGroup(group, readable, groups, stages) || !ValidPiles(world, group, piles)) return false;
        return world.EncounterGroups.Count(group => group.Active) <= 1;
    }

    private static bool ValidGroup(EncounterGroupDescriptor group, HashSet<int> readable,
        HashSet<int> groups, HashSet<int> stages)
    {
        if (group is null || string.IsNullOrWhiteSpace(group.Title) || group.Stages is null
            || !groups.Add(group.Group) || !readable.Contains(group.Group)) return false;
        if (!ValidStages(group, readable, stages))
            return false;
        if (group.Scheme is { } scheme && !readable.Contains(scheme)) return false;
        return ValidCurrent(group);
    }

    private static bool HasAreas(WorldDescriptor world) =>
        world.Areas is not null && world.Areas.All(area =>
            area is not null && area.Cards is not null && area.Removed is not null);

    private static bool ValidStages(EncounterGroupDescriptor group, HashSet<int> readable, HashSet<int> stages) =>
        group.Stages.Contains(group.Group)
        && group.Stages.All(id => readable.Contains(id) && stages.Add(id));

    private static bool ValidCurrent(EncounterGroupDescriptor group) =>
        group.Defeated == (group.Villain is null)
        && (!group.Active || group.Villain is not null)
        && (group.Villain is not { } villain || group.Stages.Contains(villain));

    private static bool ValidPiles(WorldDescriptor world, EncounterGroupDescriptor group, HashSet<int> piles) =>
        piles.Add(group.Deck) && piles.Add(group.Discard)
        && world.Areas.Count(area => area.Id == group.Deck && area.Zone == "EncounterDeck") == 1
        && world.Areas.Count(area => area.Id == group.Discard && area.Zone == "EncounterDiscardPile") == 1;
}
