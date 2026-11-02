using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the table's current draft description and last authoritative receipt.</summary>
internal sealed class BoardContextualCopy
{
    private WorldDescriptor? world;
    private Label? summary;
    private Control? resolution;
    private Label? receipt;
    private string fallback = string.Empty;

    internal void RegisterWorld(WorldDescriptor value) => world = value;
    internal void RegisterResult(Label label) => receipt = label;
    internal void RegisterSummary(Label label, string text, Control resolutionLabel)
    {
        summary = label;
        resolution = resolutionLabel;
        fallback = text;
    }

    internal void PresentResult(string text)
    {
        if (InteractionControl.IsUsable(receipt)) receipt!.Text = text;
    }

    internal void PresentDraft(DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (!InteractionControl.IsUsable(summary)) return;
        if (InteractionControl.IsUsable(resolution))
            resolution!.Visible = HasContent(resolution) && ShowsResolution(composer);
        string text = CardPaymentPresentation.UsesModal(composer) ? "" : TableDraftSummary.From(composer, prompt, world, compact: true)
            ?? ContextualOfferDescription.SingleResponse(prompt, composer?.Prompt.PublicKind) ?? fallback;
        summary!.Text = text;
        summary.Visible = text.Length > 0;
        summary.AccessibilityDescription = text;
    }

    private static bool HasContent(Control resolution) =>
        resolution is Label label ? label.Text.Length > 0 : resolution.GetChildCount() > 0;

    internal static bool ShowsResolution(DecisionComposer? composer) =>
        composer?.Selected is null || composer.Prompt.PublicKind != PublicDecisionKind.PlayerAction;

    internal void PresentFeedback(string text)
    {
        if (!InteractionControl.IsUsable(summary)) return;
        summary!.Text = text;
        summary.Visible = text.Length > 0;
        summary.AccessibilityDescription = text;
    }
}
