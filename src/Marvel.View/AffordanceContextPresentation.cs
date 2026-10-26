namespace Marvel.View;

/// <summary>Names authorized causal instances without interpreting their rules.</summary>
internal static class AffordanceContextPresentation
{
    internal static string? Describe(IReadOnlyList<int> ids, WorldDescriptor world)
    {
        string[] names = ids.Select(id => DescribeCard(id, world)).OfType<string>().ToArray();
        return names.Length == 0 ? null : string.Join("; ", names);
    }

    private static string? DescribeCard(int id, WorldDescriptor world)
    {
        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate =>
            candidate.Cards.Concat(candidate.Removed).Any(card => card.Id == id));
        CardDescriptor? card = area?.Cards.Concat(area.Removed).FirstOrDefault(candidate => candidate.Id == id);
        if (area is null || card?.Face is null) return null;
        string title = AffordanceSourcePresentation.Name(area, card, PromptPresentation.Describe(id, world));
        string location = PromptPresentation.Words(area.Zone);
        string? player = world.Players.FirstOrDefault(candidate => candidate.Seat == area.Owner)?.Name;
        return player is null ? $"{title} · {location}" : $"{title} · {player} · {location}";
    }
}
