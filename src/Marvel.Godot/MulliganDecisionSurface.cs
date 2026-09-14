using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Renders the opening-hand draft and its optional complete choice sheet.</summary>
internal static class MulliganDecisionSurface
{
    internal static void AddDraft(
        DecisionPanel panel,
        Affordance selected,
        DecisionProgressPresentation progress)
    {
        panel.AddContent(DecisionPanel.Text(
            $"OPENING HAND  ·  {panel.composer!.Targets.Count} SELECTED  ·  "
            + (progress.IsReady ? "READY" : "INCOMPLETE"),
            progress.IsReady ? GodotThemeVariations.StatusText : GodotThemeVariations.DangerText));
        if (panel.mulliganChoiceSheetOpen)
        {
            AddSheet(panel, selected, progress);
        }
        else
        {
            AddReviewButton(panel);
        }

        new DecisionPaymentRenderer(panel, panel.composer, panel.world!, panel.submitting,
            panel.GetRenderGeneration()).AddSubmit(progress);
    }

    private static void AddSheet(
        DecisionPanel panel,
        Affordance selected,
        DecisionProgressPresentation progress)
    {
        int generation = panel.GetRenderGeneration();
        new DecisionDraftRenderer(panel, panel.composer!, panel.world!, panel.submitting, generation)
            .AddTargets(selected, progress.Targets);
        var close = new Button
        {
            Name = "CloseChoiceSheet",
            Text = "Return to opening hand",
            Disabled = panel.submitting,
        };
        panel.StyleButton(close, panel.submitting
            ? InteractiveVisualState.Unavailable : InteractiveVisualState.Resting);
        close.Pressed += () => CloseSheet(panel, generation);
        panel.AddContent(close);
    }

    private static void AddReviewButton(DecisionPanel panel)
    {
        int generation = panel.GetRenderGeneration();
        var review = new Button
        {
            Name = "CompleteChoiceSheet",
            Text = "Review all opening choices",
            TooltipText = "Open the complete ordered choice list. Table selection and this list share one draft.",
            Disabled = panel.submitting,
        };
        panel.StyleButton(review, panel.submitting
            ? InteractiveVisualState.Unavailable : InteractiveVisualState.Resting);
        review.Pressed += () => OpenSheet(panel, generation);
        panel.AddContent(review);
    }

    private static void OpenSheet(DecisionPanel panel, int generation)
    {
        if (panel.IsCurrentDraft(panel.composer!, generation))
        {
            panel.mulliganChoiceSheetOpen = true;
            panel.Rebuild(focusFirst: true);
        }
    }

    private static void CloseSheet(DecisionPanel panel, int generation)
    {
        if (panel.IsCurrentDraft(panel.composer!, generation))
        {
            panel.mulliganChoiceSheetOpen = false;
            panel.Rebuild(focusFirst: true);
            Callable.From(() => FocusOpeningHand(panel)).CallDeferred();
        }
    }

    private static void FocusOpeningHand(DecisionPanel panel)
    {
        if (!panel.mulliganChoiceSheetOpen
            && panel.FindChild("CompleteChoiceSheet", recursive: true, owned: false) is Button review
            && !review.Disabled)
        {
            review.GrabFocus();
        }
    }
}
