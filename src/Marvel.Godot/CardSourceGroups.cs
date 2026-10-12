using Marvel.View;

namespace Marvel.Godot;

/// <summary>Groups current readable physical sources only by their authorized relationship.</summary>
internal static class CardSourceGroups
{
    internal static BoardCardPresentation[] Current(IEnumerable<BoardAreaPresentation> areas) =>
        [.. areas.SelectMany(area => area.Cards).Where(card => !card.Concealed && card.TargetId.HasValue
            && card.StageRole != BoardStageRole.Upcoming && card.Persistent is not null)
            .DistinctBy(card => card.TargetId)];

    internal static BoardCardPresentation[] Attached(IEnumerable<BoardAreaPresentation> areas, int host) =>
        [.. Current(areas).Where(card => card.Persistent!.Relation is { Kind: "Attached" } relation && relation.HostId == host)];

    internal static BoardCardPresentation[] Controlled(IEnumerable<BoardAreaPresentation> areas, int seat) =>
        [.. Current(areas).Where(card => card.Kind is "UPGRADE" or "SUPPORT"
            && card.Persistent!.Relation is { Kind: "Controlled" } relation && relation.Controller == seat).OrderBy(card => card.TargetId)];

    internal static bool IsLocalSource(BoardCardPresentation card) =>
        !card.Concealed && (card.Persistent?.Relation.Kind == "Attached"
            || (card.Kind is "UPGRADE" or "SUPPORT") && card.Persistent?.Relation.Kind == "Controlled");
}
