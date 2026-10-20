using Marvel.Rules.Play;
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
            gesture => CanUseTable(panel)
                && BoardDragInteractionBinder.CanDrag(panel, composer, gesture),
            gesture => CanUseTable(panel) && Activate(panel, composer, gesture),
            gesture => CanUseTable(panel)
                && BoardDragInteractionBinder.Drag(panel, composer, board, gesture));
        board.BindDragPreview(gesture => BoardDragInteractionBinder.PreviewDrag(panel, composer, board, gesture));
        board.SetInlinePayment(panel.LocalDecision.IsOpen);
        board.SetDialogOnly(DecisionDialogLayout.Automatic(composer.Prompt) || panel.LocalDecision.Active);
        board.SetCompleteChoicesOpen(panel.CompleteChoicesOpen || panel.AbilityWindowOpen);
        board.BindCompleteChoices(source =>
        {
            if (CanUseTable(panel)) panel.ShowCompleteChoices(source);
        });
        board.BindExplicitInteraction(gesture => CanUseTable(panel)
            && Activate(panel, composer, gesture));
        board.BindContextualInteraction(
            id => SelectContextAction(panel, composer, id),
            () => Decline(panel, composer, panel.GetRenderGeneration()),
            () => Submit(panel, composer, panel.GetRenderGeneration()),
            () => CancelDraft(panel, composer),
            index => SelectCost(panel, composer, index),
            () => DecisionPanelCopy.SubmitAction(composer, panel.world!));
    }

    private static bool CanUseTable(DecisionPanel panel) =>
        !panel.AcknowledgementPending && !panel.submitting;

    private static void SelectContextAction(DecisionPanel panel, DecisionComposer composer, int id)
    {
        int generation = panel.GetRenderGeneration();
        if (panel.PaymentModalOpen || panel.AcknowledgementPending
            || !CurrentOperations(panel, composer).TrySelectAffordance(id)) return;
        panel.LocalDecision.LeaveForTableAction();
        panel.RaiseDraftStarted();
        if (DirectCommitment.IsReady(composer))
        {
            Submit(panel, composer, generation);
            return;
        }
        panel.Rebuild();
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

        if (gesture.Intent == CardInteractionIntent.ContextMenu)
            return CardContextMenuBinding.Show(panel, composer, gesture);
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
                panel, composer, generation, gesture.Source, interaction, cardId),
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
            || !composer.TryBuild(out Decision? decision, out _))
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
            || !composer.TryDecline(out Decision? decision, out _))
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
        Control source,
        BoardDraftInteraction interaction,
        int cardId)
    {
        IReadOnlyList<Marvel.View.AffordancePresentation> actions = interaction.VisibleActions(cardId);
        if (actions.Count == 1)
        {
            if (RecipientCommitmentChoices.Entries(actions[0]) is { Count: > 0 } recipients)
            {
                panel.BoardChoices.Show(source, composer, cardId, recipients, recipient =>
                {
                    if (RecipientCommitmentChoices.Select(composer, CurrentOperations(panel, composer), actions[0].Id, recipient))
                    {
                        panel.LocalDecision.LeaveForTableAction();
                        Submit(panel, composer, generation);
                    }
                }, replacement => ReopenSourceChoice(panel, composer, cardId, replacement), recipients: true);
                return BoardDraftMutation.None;
            }
            if (panel.BoardChoices.IsRestoring) return BoardDraftMutation.None;
            BoardDraftMutation mutation = SelectFreshAction(panel, interaction, actions[0].Id, cardId);
            return mutation == BoardDraftMutation.Affordance
                && DirectCommitment.IsReady(composer)
                ? Submit(panel, composer, generation) : mutation;
        }
        if (actions.Count > 1)
        {
            panel.BoardChoices.Show(source, composer, cardId, actions, id =>
            {
                if (SelectFreshAction(panel, interaction, id, cardId) == BoardDraftMutation.Affordance)
                {
                    panel.RaiseDraftStarted();
                    if (DirectCommitment.IsReady(composer)) Submit(panel, composer, generation);
                    else RefreshDraft(panel, composer, generation, cardId);
                }
            }, replacement => ReopenSourceChoice(panel, composer, cardId, replacement));
        }
        return BoardDraftMutation.None;
    }

    private static BoardDraftMutation SelectFreshAction(
        DecisionPanel panel, BoardDraftInteraction interaction, int offer, int card)
    {
        BoardDraftMutation mutation = interaction.TrySelectAction(offer, card);
        if (mutation == BoardDraftMutation.Affordance) panel.LocalDecision.LeaveForTableAction();
        return mutation;
    }

    private static void ReopenSourceChoice(DecisionPanel panel, DecisionComposer composer, int source, Control replacement)
    {
        var interaction = new BoardDraftInteraction(composer, CurrentOperations(panel, composer), Affordances(panel, composer));
        SelectAction(panel, composer, panel.GetRenderGeneration(), replacement, interaction, source);
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
