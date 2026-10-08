using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Identifies the unpaid card and preserves the current resolution context.</summary>
internal static class CardPaymentWorkspaceHeader
{
    internal static void Add(VBoxContainer content, DecisionPanel panel, Main main,
        DecisionComposer draft, PromptPresentation prompt)
    {
        string title = PromptPresentation.Describe(panel.composer!.Selected!.AnchorId, panel.world!);
        var identity = new HBoxContainer { Name = "PaymentIdentity" };
        var heading = DecisionPanel.Text($"Play {title}", GodotThemeVariations.Heading, wrap: true);
        heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        identity.AddChild(heading);
        identity.AddChild(CardPaymentInspection.Button(panel, main, draft.Selected!.AnchorId, "Inspect played card"));
        content.AddChild(identity);
        if (ShowsResolution(draft.Prompt.PublicKind, draft.Prompt.CauseCardIds.Count > 0)
            && !string.IsNullOrWhiteSpace(prompt.Resolution))
            content.AddChild(DecisionPanel.Text(prompt.Resolution, GodotThemeVariations.Caption, wrap: true));
        if (draft.Selected!.DeferredTargetSelection)
            content.AddChild(DecisionPanel.Text(
                "Payment commits now. Choose a target as the effect resolves; cancelling a later draft does not refund payment.",
                GodotThemeVariations.Caption, wrap: true));
    }

    internal static bool ShowsResolution(PublicDecisionKind kind, bool hasCause) =>
        kind != PublicDecisionKind.PlayerAction || hasCause;
}
