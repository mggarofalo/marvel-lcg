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
            gesture => CanDrag(panel, composer, gesture),
            gesture => Activate(panel, composer, gesture),
            gesture => Drag(panel, composer, board, gesture));
        board.BindExplicitInteraction(gesture => Activate(panel, composer, gesture));
        board.BindContextualInteraction(id => panel.SelectAffordance(id, panel.GetRenderGeneration()));
    }

    private static bool Activate(
        DecisionPanel panel,
        DecisionComposer composer,
        CardPointerGesture gesture)
    {
        if (gesture.Card.TargetId is not { } cardId)
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
        if (!panel.IsCurrentDraft(composer, generation)
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
        if (!panel.IsCurrentDraft(composer, generation)
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
        RefreshDraft(panel, composer, panel.GetRenderGeneration(), gesture.Card.TargetId!.Value);
        return true;
    }

    private static bool CanDrag(
        DecisionPanel panel,
        DecisionComposer composer,
        CardPointerGesture gesture) => new BoardDraftInteraction(
            composer,
            CurrentOperations(panel, composer),
            Affordances(panel, composer)).CanPlay(
                gesture.Card.TargetId,
                gesture.IsHandCard);

    private static TableDraftBinding CurrentOperations(
        DecisionPanel panel, DecisionComposer composer) =>
        panel.BindTableDraft(composer, panel.GetRenderGeneration());

    private static IReadOnlyList<Marvel.View.AffordancePresentation> Affordances(
        DecisionPanel panel, DecisionComposer composer) =>
        Marvel.View.PromptPresentation.From(composer.Prompt, panel.world!).Affordances;

    private static void RefreshDraft(
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

    private static void RestoreCardFocus(
        DecisionPanel panel,
        DecisionComposer composer,
        int generation,
        int cardId)
    {
        if (!panel.IsCurrentDraft(composer, generation)) return;
        Button[] sameCard = [.. panel.GetTree().Root
            .FindChildren($"Card{cardId}*", "Button", true, false)
            .OfType<Button>()
            .Where(button => InteractionControl.IsUsable(button)
                && button.IsVisibleInTree() && !button.Disabled)];
        Button? candidate = sameCard.LastOrDefault() ?? panel.GetTree().Root
            .FindChildren("Card*", "Button", true, false)
            .OfType<Button>()
            .LastOrDefault(button => InteractionControl.IsUsable(button)
                && button.IsVisibleInTree() && !button.Disabled
                && button.HasMeta("spatial_card_anchor"));
        if (candidate is not null)
        {
            candidate.GrabFocus();
            InteractionControl.ResetDisabledScrollAncestors(candidate);
        }
    }
}
