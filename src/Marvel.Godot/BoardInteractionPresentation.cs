using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Prompt-owned mappings that may be exposed beside visible board objects.</summary>
public sealed record BoardInteractionPresentation(
    Prompt? Prompt,
    int? SelectedAffordance,
    IReadOnlyList<int> SelectedTargets)
{
    /// <summary>Actions mapped by the prompt to one visible object.</summary>
    public IReadOnlyList<Affordance> ActionsFor(int? targetId) => targetId is null
        ? []
        : [.. Prompt?.Affordances.Where(option => option.AnchorId == targetId.Value) ?? []];

    /// <summary>Whether the selected prompt maps an ordinary target to this object.</summary>
    public bool CanTarget(int? targetId)
    {
        if (targetId is null || SelectedAffordance is not { } selected)
        {
            return false;
        }

        TargetRequest? request = Prompt?.Affordances
            .FirstOrDefault(option => option.Id == selected)?.Targets;
        return request is
        {
            IsGrouped: false,
            IsSearch: false,
            AllowRepeated: false,
        } && request.Legal.Contains(targetId.Value);
    }

    public bool IsSelected(int? targetId) =>
        targetId is { } id && SelectedTargets.Contains(id);
}
