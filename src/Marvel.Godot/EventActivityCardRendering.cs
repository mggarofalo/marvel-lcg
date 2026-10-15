using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Refreshes local, upright receipt values without reallocating a card's table slot.</summary>
internal static class EventActivityCardRendering
{
    internal static void Apply(CardControl card, BoardCardPresentation presentation)
    {
        if (card.DisplayedPresentation?.FaceId != presentation.FaceId)
            EventActivityFaceRendering.Apply(card, presentation);
        card.DisplayedPresentation = presentation;
        card.AccessibilityName = CardLiveStateRendering.Description(presentation);
        card.TooltipText = string.Empty;
        RefreshSourceHeader(card, presentation);
        RefreshLiveState(card, presentation);
        card.SetMeta("activity_health", presentation.Fields.FirstOrDefault(field => field.Name == "HEALTH")?.Value ?? "");
        card.SetMeta("activity_readiness", presentation.Status);
    }

    private static void RefreshSourceHeader(CardControl card, BoardCardPresentation presentation)
    {
        if (card.GetNodeOrNull<Control>("SourceBody/SourceTitleBand") is Control header)
        {
            Node parent = header.GetParent();
            int index = header.GetIndex();
            parent.RemoveChild(header);
            header.QueueFree();
            Control replacement = SourceTileHeader.Create(presentation, card.GetMeta("source_copy", "").AsString(),
                (InterfaceScale)card.GetMeta("source_reading_scale", 100).AsInt32());
            parent.AddChild(replacement);
            parent.MoveChild(replacement, index);
        }
    }

    private static void RefreshLiveState(CardControl card, BoardCardPresentation presentation)
    {
        if (card.GetNodeOrNull<Control>("SpatialOverlay/LiveStatePanel") is Control old)
        {
            old.GetParent().RemoveChild(old);
            old.QueueFree();
            SpatialCardSidecar.State(card, presentation);
        }
        else if ((card.GetNodeOrNull<Control>("SourceBody") ?? card.GetNodeOrNull<Control>("CardSurface"))?.FindChild("LiveProgressValue", true, false) is Label value
            && CardProgressValue.From(presentation) is { } progress)
            value.Text = progress.Value;
    }
}
