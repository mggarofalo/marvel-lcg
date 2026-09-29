using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Maintains the hidden opening-hand composer surface used by table controls.</summary>
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
        new DecisionPaymentRenderer(panel, panel.composer, panel.world!, panel.submitting,
            panel.GetRenderGeneration()).AddSubmit(progress);
    }
}
