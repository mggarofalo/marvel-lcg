using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Exports topology relationships only after the ordinary card visibility filter.</summary>
internal static class EncounterGroupProjection
{
    internal static IReadOnlyList<EncounterGroupDescriptor> From(World world, WorldDescriptor visible)
    {
        if (world.EncounterTopology is not { } topology) return [];
        var readable = visible.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null && card.Face is not null)
            .ToDictionary(card => card.Id!.Value);
        var result = new List<EncounterGroupDescriptor>();
        foreach (var group in topology.Groups)
        {
            // Group identity is its initial physical stage, never a concealed deck member.
            // Synthetic concealed group identities omit the relationship rather than leaking it.
            if (!readable.TryGetValue(group.Villain.ObjectId, out var initial)) continue;
            var current = group.CurrentVillain;
            if (current is not null && !readable.ContainsKey(current.ObjectId)) continue;
            result.Add(new EncounterGroupDescriptor(group.Villain.ObjectId, initial.Face!.Title,
                current is not null && readable.ContainsKey(current.ObjectId) ? current.ObjectId : null,
                readable.ContainsKey(group.Scheme.ObjectId) ? group.Scheme.ObjectId : null,
                group.Deck.Id, group.Discard.Id)
            {
                Active = current is not null && ReferenceEquals(topology.Active, group),
                Defeated = current is null,
                Stages = group.Stages.Where(card => readable.ContainsKey(card.ObjectId))
                    .Select(card => card.ObjectId).ToArray(),
            });
        }
        return result.ToArray();
    }
}
