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
            gesture => BoardDragInteractionBinder.CanDrag(panel, composer, gesture),
            gesture => Activate(panel, composer, gesture),
            gesture => BoardDragInteractionBinder.Drag(panel, composer, board, gesture));
        board.BindDragPreview(gesture => BoardDragInteractionBinder.PreviewDrag(panel, composer, board, gesture));
        board.SetCompleteChoicesOpen(panel.CompleteChoicesOpen);
        board.BindCompleteChoices(source => panel.ShowCompleteChoices(source));
        board.BindExplicitInteraction(gesture => Activate(panel, composer, gesture));
        board.BindContextualInteraction(
            id => { if (!panel.PaymentModalOpen) panel.SelectAffordance(id, panel.GetRenderGeneration()); },
            () => Decline(panel, composer, panel.GetRenderGeneration()),
            () => Submit(panel, composer, panel.GetRenderGeneration()),
            () => CancelDraft(panel, composer),
            index => SelectCost(panel, composer, index),
            () => DecisionPanelCopy.SubmitAction(composer, panel.world!));
    }

    private static void SelectCost(DecisionPanel panel, DecisionComposer composer, int index)
    {
        if (!panel.PaymentModalOpen && CurrentOperations(panel, composer).TrySelectCost(index)) panel.Rebuild();
    }

    private static void CancelDraft(DecisionPanel panel, DecisionComposer composer)
    {
        if (panel.PaymentModalOpen || !panel.IsCurrentDraft(composer, panel.GetRenderGeneration())
            || composer.Selected is not { } selected) return;
        int anchor = selected.AnchorId;
        var fresh = BoardDraftCancellation.Clear(composer);
        panel.composer = fresh;
        panel.Rebuild();
        int generation = panel.GetRenderGeneration();
        Callable.From(() => RestoreCardFocus(panel, fresh, generation, anchor)).CallDeferred();
    }

    private static bool Activate(
        DecisionPanel panel,
        DecisionComposer composer,
        CardPointerGesture gesture)
    {
        if (panel.PaymentModalOpen || gesture.Card.TargetId is not { } cardId)
        {
            return false;
        }

        int generation = panel.GetRenderGeneration();
        var interaction = new BoardDraftInteraction(
            composer, CurrentOperations(panel, composer), Affordances(panel, composer));
        BoardDraftMutation mutation = gesture.Intent switch
        {
            CardInteractionIntent.Target => interaction.TryToggleTarget(cardId),
            CardInteractionIntent.Generator => interaction.TryToggleGenerator(cardId),
            CardInteractionIntent.Cost => gesture.Option is { } cost
                && CurrentOperations(panel, composer).TrySelectCost(cost)
                    ? BoardDraftMutation.Cost
                    : BoardDraftMutation.None,
            CardInteractionIntent.Submit => Submit(panel, composer, generation),
            CardInteractionIntent.Decline => Decline(panel, composer, generation),
            CardInteractionIntent.Action => SelectAction(
                panel, composer, generation, gesture, interaction, cardId),
            _ => BoardDraftMutation.None,
        };
        if (mutation == BoardDraftMutation.None)
        {
            // More than one action opens the source-local choice surface;
            // list order is never a choice rule.
            return false;
        }

        if (mutation == BoardDraftMutation.Affordance)
        {
            panel.RaiseDraftStarted();
        }
        RefreshDraft(panel, composer, generation, cardId);
        return true;
    }

    private static BoardDraftMutation Submit(
        DecisionPanel panel, DecisionComposer composer, int generation)
    {
        if (panel.PaymentModalOpen || !panel.IsCurrentDraft(composer, generation)
            || !composer.TryBuild(out EngineDecision? decision, out _))
        {
            return BoardDraftMutation.None;
        }
        panel.NotifySubmitted(decision!, generation);
        return BoardDraftMutation.Submit;
    }

    private static BoardDraftMutation Decline(
        DecisionPanel panel, DecisionComposer composer, int generation)
    {
        if (panel.PaymentModalOpen || !panel.IsCurrentDraft(composer, generation)
            || !composer.TryDecline(out EngineDecision? decision, out _))
        {
            return BoardDraftMutation.None;
        }
        panel.NotifySubmitted(decision!, generation);
        return BoardDraftMutation.Decline;
    }

    private static BoardDraftMutation SelectAction(
        DecisionPanel panel,
        DecisionComposer composer,
        int generation,
        CardPointerGesture gesture,
        BoardDraftInteraction interaction,
        int cardId)
    {
        IReadOnlyList<Marvel.View.AffordancePresentation> actions = interaction.VisibleActions(cardId);
        if (actions.Count == 1)
        {
            return interaction.TrySelectAction(actions[0].Id, cardId);
        }
        if (actions.Count > 1)
        {
            BoardActionChoiceSurface.Show(gesture.Source, actions,
                () => panel.IsCurrentDraft(composer, panel.GetRenderGeneration()), id =>
            {
                if (interaction.TrySelectAction(id, cardId) == BoardDraftMutation.Affordance)
                {
                    panel.RaiseDraftStarted();
                    RefreshDraft(panel, composer, generation, cardId);
                }
            });
        }
        return BoardDraftMutation.None;
    }

    private static TableDraftBinding CurrentOperations(
        DecisionPanel panel, DecisionComposer composer) =>
        panel.BindTableDraft(composer, panel.GetRenderGeneration());

    private static IReadOnlyList<Marvel.View.AffordancePresentation> Affordances(
        DecisionPanel panel, DecisionComposer composer) =>
        Marvel.View.PromptPresentation.From(composer.Prompt, panel.world!).Affordances;

    internal static void RefreshDraft(
        DecisionPanel panel, DecisionComposer composer, int generation, int focused)
    {
        Callable.From(() =>
        {
            if (!panel.IsCurrentDraft(composer, generation))
            {
                return;
            }
            panel.NotifyAnchorFocused([focused]);
            panel.Rebuild();
            int refreshedGeneration = panel.GetRenderGeneration();
            Callable.From(() => RestoreCardFocus(
                    panel, composer, refreshedGeneration, focused))
                .CallDeferred();
        }).CallDeferred();
    }

    internal static void RestoreCardFocus(
        DecisionPanel panel, DecisionComposer composer, int generation, int cardId) =>
        BoardControlFocusRestore.Restore(panel, composer, generation, cardId);

}
