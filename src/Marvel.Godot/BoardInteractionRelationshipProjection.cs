using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Chooses the prompt relationship set without manufacturing table links.</summary>
internal static class BoardInteractionRelationshipProjection
{
    internal static IReadOnlyList<TableRelationshipDescriptor> From(
        DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (composer?.Selected is not { } selected || prompt is null)
        {
            return [];
        }

        return prompt.Affordances.SingleOrDefault(affordance => affordance.Id == selected.Id)
            ?.Relationships ?? [];
    }
}
