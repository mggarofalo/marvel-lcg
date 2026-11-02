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
    private HFlowContainer? facts;
    private Control? resolution;
    private Label? receipt;
    private string fallback = string.Empty;

    internal void RegisterWorld(WorldDescriptor value) => world = value;
    internal void RegisterResult(Label label) => receipt = label;
    internal void RegisterSummary(Label label, HFlowContainer draftFacts, string text, Control resolutionLabel)
    {
        summary = label;
        facts = draftFacts;
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
        (IReadOnlyList<DraftPart>? parts, string text) = Draft(composer, prompt);
        summary!.Text = text;
        summary.AccessibilityDescription = text;
        bool drawn = parts is { Count: > 0 } && InteractionControl.IsUsable(facts);
        if (InteractionControl.IsUsable(facts)) DraftFactsRow.Fill(facts!, drawn ? parts! : []);
        summary.Visible = text.Length > 0 && !drawn;
    }

    // A card play's draft is in its payment surface; any other draft is its parts, or a single response's words.
    private (IReadOnlyList<DraftPart>? Parts, string Text) Draft(DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (CardPaymentPresentation.UsesModal(composer)) return (null, "");
        IReadOnlyList<DraftPart>? parts = TableDraftSummary.Parts(composer, prompt, world, compact: true);
        return parts is not null ? (parts, string.Join("  ·  ", parts.Select(part => part.Text)))
            : (null, ContextualOfferDescription.SingleResponse(prompt, composer?.Prompt.PublicKind) ?? fallback);
    }

    private static bool HasContent(Control resolution) =>
        resolution is Label label ? label.Text.Length > 0 : resolution.GetChildCount() > 0;

    internal static bool ShowsResolution(DecisionComposer? composer) =>
        composer?.Selected is null || composer.Prompt.PublicKind != PublicDecisionKind.PlayerAction;

    internal void PresentFeedback(string text)
    {
        if (!InteractionControl.IsUsable(summary)) return;
        if (InteractionControl.IsUsable(facts)) DraftFactsRow.Fill(facts!, []);
        summary!.Text = text;
        summary.Visible = text.Length > 0;
        summary.AccessibilityDescription = text;
    }
}
