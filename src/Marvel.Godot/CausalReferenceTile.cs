using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A source reference for the current situation, distinct from an installed upgrade.</summary>
internal static class CausalReferenceTile
{
    internal static CardControl Create(BoardCardPresentation card, float width, InterfaceScale scale)
    {
        CardControl control = CardControl.CreateSource(card, width, scale);
        var body = control.GetNode<VBoxContainer>("SourceBody");
        Node strip = body.GetNode("SourceStrip");
        body.RemoveChild(strip);
        strip.QueueFree();
        var content = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(content);
        body.MoveChild(content, 0);
        content.AddChild(CardSourceStrip.Label(card.Title, "ReferenceTitle", 16, CardTypography.Title));
        content.AddChild(CardSourceStrip.Label(card.Kind, "ReferenceKind", 10, CardTypography.Body));
        if (CardProgressValue.From(card) is { } progress)
            content.AddChild(CardSourceStrip.Label($"{(progress.IsThreat ? "Threat" : "HP")} {progress.Value}",
                "ReferenceProgress", 13, CardTypography.Bold));
        control.AccessibilityName = CardLiveStateRendering.Description(card);
        return control;
    }
}
