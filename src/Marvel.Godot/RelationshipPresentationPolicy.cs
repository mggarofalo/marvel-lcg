using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps unfocused decorative table links sparse while prompt controls retain their text.</summary>
internal static class RelationshipPresentationPolicy
{
    internal static IReadOnlyList<TableRelationshipDescriptor> SparseUnfocused(
        IReadOnlyList<TableRelationshipDescriptor> relationships)
    {
        ArgumentNullException.ThrowIfNull(relationships);
        IReadOnlyList<TableRelationshipDescriptor> cardLinks = relationships
            .Where(relationship => relationship.Related is not null)
            .ToArray();
        Dictionary<int, int> subjects = cardLinks
            .GroupBy(relationship => relationship.Subject)
            .ToDictionary(group => group.Key, group => group.Count());
        Dictionary<int, int> related = cardLinks
            .GroupBy(relationship => relationship.Related!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        return cardLinks.Where(relationship => subjects[relationship.Subject] == 1
                && related[relationship.Related!.Value] == 1)
            .ToArray();
    }
}
