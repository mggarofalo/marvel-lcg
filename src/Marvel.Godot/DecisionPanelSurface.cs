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
        if (selected.Targets is not null && !composer.UsesAutomaticTargetSelection)
        {
            new DecisionDraftRenderer(panel, composer, panel.world!, panel.submitting, generation)
                .AddTargets(selected, progress.Targets);
        }
        var payment = new DecisionPaymentRenderer(panel, composer, panel.world!, panel.submitting, generation);
        payment.AddCosts(selected);
        payment.AddSubmit(composer.Progress());
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
        pass.Pressed += () =>
        {
            if (panel.IsCurrentDraft(composer, generation) && composer.TryDecline(out EngineDecision? decision, out _))
                panel.NotifySubmitted(decision!, generation);
        };
        panel.AddCommit(pass);
    }
}
