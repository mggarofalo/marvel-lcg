using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the table's current draft description and last authoritative receipt.</summary>
internal sealed class BoardContextualCopy
{
    private WorldDescriptor? world;
    private Label? summary;
    private Label? receipt;
    private string fallback = string.Empty;

    internal void RegisterWorld(WorldDescriptor value) => world = value;
    internal void RegisterResult(Label label) => receipt = label;
    internal void RegisterSummary(Label label, string text)
    {
        summary = label;
        fallback = text;
    }

    internal void PresentResult(string text)
    {
        if (InteractionControl.IsUsable(receipt)) receipt!.Text = text;
    }

    internal void PresentDraft(DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (!InteractionControl.IsUsable(summary)) return;
        string text = TableDraftSummary.From(composer, prompt, world) ?? fallback;
        summary!.Text = text;
        summary.TooltipText = text;
    }

    internal void PresentFeedback(string text)
    {
        if (InteractionControl.IsUsable(summary)) summary!.Text = text;
    }
}
