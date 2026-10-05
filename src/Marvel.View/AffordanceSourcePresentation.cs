namespace Marvel.View;

/// <summary>Names an authorized source instance and its current state without predicting effects.</summary>
internal static class AffordanceSourcePresentation
{
    internal static (string? Name, string? State) From(
        AffordanceSourceDescriptor? source, WorldDescriptor world)
    {
        if (source?.CardId is not int id) return (null, null);
        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == source.AreaId);
        CardDescriptor? card = area?.Cards.Concat(area.Removed).FirstOrDefault(candidate => candidate.Id == id);
        if (area is null || card?.Face is null) return (null, null);
        BoardCardPresentation visible = BoardCardPresentationFactory.Present(card, area.Zone);
        return (Name(area, card, visible.Title), CardStatePresentation.Summary(visible, includeTitle: false));
    }

    private static string Name(AreaDescriptor area, CardDescriptor card, string title)
    {
        CardDescriptor[] copies = [.. area.Cards.Where(candidate => candidate.Face?.Title == card.Face!.Title)];
        int ordinal = Array.FindIndex(copies, candidate => candidate.Id == card.Id);
        return copies.Length > 1 && ordinal >= 0
            ? $"{title} (copy {ordinal + 1} of {copies.Length})" : title;
    }
}
