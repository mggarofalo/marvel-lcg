using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Refreshes local, upright receipt values without reallocating a card's table slot.</summary>
internal static class EventActivityCardRendering
{
    internal static void Apply(CardControl card, BoardCardPresentation presentation)
    {
        card.DisplayedPresentation = presentation;
        card.AccessibilityName = CardLiveStateRendering.Description(presentation);
        card.TooltipText = string.Empty;
        if (card.FindChild("SourceTitleBand", true, false) is Control header)
        {
            Node parent = header.GetParent();
            int index = header.GetIndex();
            parent.RemoveChild(header);
            header.QueueFree();
            Control replacement = SourceTileHeader.Create(presentation, card.GetMeta("source_copy", "").AsString());
            parent.AddChild(replacement);
            parent.MoveChild(replacement, index);
        }
        if (card.FindChild("LiveStatePanel", true, false) is Control old)
        {
            old.GetParent().RemoveChild(old);
            old.QueueFree();
            SpatialCardSidecar.State(card, presentation);
        }
        else if (card.FindChild("LiveProgressValue", true, false) is Label value
            && CardProgressValue.From(presentation) is { } progress)
            value.Text = progress.Value;
        card.SetMeta("activity_health", presentation.Fields.FirstOrDefault(field => field.Name == "HEALTH")?.Value ?? "");
        card.SetMeta("activity_readiness", presentation.Status);
    }
}
