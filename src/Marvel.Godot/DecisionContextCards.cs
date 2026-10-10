using Marvel.View;

namespace Marvel.Godot;

/// <summary>Selects authorized causes needing a contextual table copy, without moving game objects.</summary>
internal static class DecisionContextCards
{
    internal static IReadOnlyList<BoardCardPresentation> MissingFromTable(
        PromptPresentation? prompt, BoardPresentation board)
    {
        if (prompt is null) return [];
        var tableIds = board.Areas.Where(SpatialTableZones.IsResolving)
            .SelectMany(SpatialTableZones.Current)
            .Where(card => !card.Concealed && card.TargetId is not null)
            .Select(card => card.TargetId!.Value).ToHashSet();
        return [.. prompt.ContextCards.Where(card => !card.Concealed
            && card.TargetId is { } id && !tableIds.Contains(id)).DistinctBy(card => card.TargetId)];
    }
}
