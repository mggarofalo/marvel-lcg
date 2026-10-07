using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Chooses the prompt relationship set without manufacturing table links.</summary>
internal static class BoardInteractionRelationshipProjection
{
    internal static IReadOnlyList<TableRelationshipDescriptor> From(
        DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (composer?.Selected is not { } selected || prompt is null
            || MulliganPrompt.IsOpening(composer.Prompt)
            || CardPaymentPresentation.UsesModal(composer))
        {
            return [];
        }

        return [.. (prompt.Affordances.SingleOrDefault(affordance => affordance.Id == selected.Id)
            ?.Relationships ?? [])
            .Where(relationship => IsOffered(composer, relationship))];
    }

    private static bool IsOffered(DecisionComposer composer, TableRelationshipDescriptor relationship) =>
        relationship.Related is { } related
        && (relationship.Kind == RelationshipKind.OfferedTarget
            || relationship.Kind == RelationshipKind.OfferedGenerator
            && DecisionResourceEligibility.CanToggle(composer, related));
}
