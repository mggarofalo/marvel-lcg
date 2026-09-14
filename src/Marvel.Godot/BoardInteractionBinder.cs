using Godot;
using Marvel.Decisions;
namespace Marvel.Godot;

/// <summary>Binds direct table gestures to the current prompt-bound composer draft.</summary>
internal static class BoardInteractionBinder
{
    internal static void Bind(DecisionPanel panel, BoardRenderResult? board)
    {
        if (board is null || panel.composer is not { } composer)
        {
            return;
        }

        board.BindDirectInteractions(
            gesture => Activate(panel, composer, gesture),
            gesture => Drag(panel, composer, board, gesture));
    }

    private static bool Activate(
        DecisionPanel panel,
        DecisionComposer composer,
        CardPointerGesture gesture)
    {
        if (gesture.IsHandCard)
        {
            // A hand-card body is always the inspection surface. Playing it
            // requires the deliberate lane drag below.
            return false;
        }

        BoardDraftMutation mutation = new BoardDraftInteraction(
            composer, CurrentOperations(panel, composer), Affordances(panel, composer)).TryActivate(
                gesture.Card.TargetId, gesture.IsHandCard);
        if (mutation == BoardDraftMutation.None)
        {
            // More than one action is intentionally left to the explicit
            // ordered affordance surface; list order is never a choice rule.
            return false;
        }

        if (mutation == BoardDraftMutation.Affordance)
        {
            panel.RaiseDraftStarted();
        }
        RefreshDraft(panel, gesture.Card.TargetId!.Value);
        return true;
    }

    private static bool Drag(
        DecisionPanel panel,
        DecisionComposer composer,
        BoardRenderResult board,
        CardPointerGesture gesture)
    {
        BoardDraftMutation mutation = new BoardDraftInteraction(
            composer, CurrentOperations(panel, composer), Affordances(panel, composer)).TryPlay(
                gesture.Card.TargetId, gesture.IsHandCard,
                board.IsDroppedOnLivePlayerLane(composer.Prompt.Player, gesture.Position));
        if (mutation != BoardDraftMutation.Affordance)
        {
            return false;
        }

        panel.RaiseDraftStarted();
        RefreshDraft(panel, gesture.Card.TargetId!.Value);
        return true;
    }

    private static TableDraftBinding CurrentOperations(
        DecisionPanel panel, DecisionComposer composer) =>
        panel.BindTableDraft(composer, panel.GetRenderGeneration());

    private static IReadOnlyList<Marvel.View.AffordancePresentation> Affordances(
        DecisionPanel panel, DecisionComposer composer) =>
        Marvel.View.PromptPresentation.From(composer.Prompt, panel.world!).Affordances;

    private static void RefreshDraft(DecisionPanel panel, int focused) 
    {
        panel.NotifyAnchorFocused([focused]);
        panel.Rebuild();
    }
}
