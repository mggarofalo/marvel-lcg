using Marvel.Rules.Play;
using Marvel.View;
using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Composes the selected answer and explicit commitment controls.</summary>
internal static class DecisionPanelSurface
{
    internal static void AddSelectedDraft(DecisionPanel panel)
    {
        if (panel.composer is not { Selected: { } selected } composer) return;
        DecisionProgressPresentation progress = composer.Progress();
        if (MulliganPrompt.IsOpening(composer.Prompt))
        {
            MulliganDecisionSurface.AddDraft(panel, selected, progress);
            return;
        }
        int generation = panel.GetRenderGeneration();
        bool deferredPayment = panel.LocalDecision.IsOpen && selected.PlaysCard;
        if (!deferredPayment) AddTargets(panel, composer, selected, progress, generation);
        var payment = new DecisionPaymentRenderer(panel, composer, panel.world!, panel.submitting, generation);
        payment.AddCosts(selected);
        if (deferredPayment) AddTargets(panel, composer, selected, progress, generation);
        payment.AddSubmit(composer.Progress());
    }

    private static void AddTargets(DecisionPanel panel, DecisionComposer composer, Affordance selected,
        DecisionProgressPresentation progress, int generation)
    {
        if (selected.Targets is not null && !composer.UsesAutomaticTargetSelection)
        {
            if (!panel.CardChoices.AddVisibleTargets(generation))
                new DecisionDraftRenderer(panel, composer, panel.world!, panel.submitting, generation)
                    .AddTargets(selected, progress.Targets);
        }
    }

    internal static void AddDecline(DecisionPanel panel)
    {
        if (panel.composer?.Prompt.Cancellable != true) return;
        DecisionComposer composer = panel.composer;
        int generation = panel.GetRenderGeneration();
        var pass = new Button
        {
            Name = "Decline", Text = composer.Prompt.DeclineLabel, Disabled = panel.submitting,
        };
        panel.StyleButton(pass, panel.submitting ? InteractiveVisualState.Unavailable : InteractiveVisualState.Resting);
        pass.SetMeta("table_mutation", true);
        pass.Pressed += () =>
        {
            if (panel.IsCurrentDraft(composer, generation) && composer.TryDecline(out Decision? decision, out _))
                panel.NotifySubmitted(decision!, generation);
        };
        panel.AddCommit(pass);
    }
    internal static void RenderNoDecision(DecisionPanel panel)
    {
        (string heading, string detail) = panel.world?.Outcome switch
        {
            Outcome.Unfinished => (
                "WAITING FOR ANOTHER PLAYER",
                "Another player has the current decision."),
            Outcome.PlayersWin => ("VICTORY", "The players won. No further decision is waiting."),
            Outcome.VillainWins => ("DEFEAT", "The villain won. No further decision is waiting."),
            Outcome.PlayersLose => ("DEFEAT", "The players lost. No further decision is waiting."),
            _ => ("NO DECISION", "No decision is available."),
        };
        if (panel.world is { Outcome: not Outcome.Unfinished })
            detail = OutcomeDescription.Result(panel.world.Outcome, panel.world.LossReason);
        panel.AddChild(DecisionPanel.Text(heading, GodotThemeVariations.Caption));
        panel.AddChild(DecisionPanel.Text(detail, GodotThemeVariations.Body, wrap: true));
    }


}
