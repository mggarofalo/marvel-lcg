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
    private Label? resolution;
    private Label? receipt;
    private string fallback = string.Empty;

    internal void RegisterWorld(WorldDescriptor value) => world = value;
    internal void RegisterResult(Label label) => receipt = label;
    internal void RegisterSummary(Label label, string text, Label resolutionLabel)
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
            resolution!.Visible = resolution.Text.Length > 0 && ShowsResolution(composer);
        string text = TableDraftSummary.From(composer, prompt, world, compact: true)
            ?? ContextualOfferDescription.SingleResponse(prompt, composer?.Prompt.PublicKind) ?? fallback;
        summary!.Text = text;
        summary.Visible = text.Length > 0;
        summary.TooltipText = text;
    }

    internal static bool ShowsResolution(DecisionComposer? composer) =>
        composer?.Selected is null || composer.Prompt.PublicKind != PublicDecisionKind.PlayerAction;

    internal void PresentFeedback(string text)
    {
        if (!InteractionControl.IsUsable(summary)) return;
        summary!.Text = text;
        summary.Visible = text.Length > 0;
    }
}
