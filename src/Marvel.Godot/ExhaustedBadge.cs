using Godot;

namespace Marvel.Godot;

/// <summary>
/// An exhausted card's mark: the card stays upright and readable, and a badge at its top edge carries the
/// exhausted glyph, named on hover. Whether a card is exhausted is the engine's answer.
/// </summary>
/// <remarks>
/// Showing exhaustion as a badge on an upright card, rather than a quarter turn, is our choice; it keeps
/// every face readable at the table's sizes. The glyph is the one the source tiles already use.
/// </remarks>
internal static class ExhaustedBadge
{
    internal const string NodeName = "ExhaustedBadge";

    /// <summary>The badge's side: two body lines at the reading scale, never smaller than a pointer target's half.</summary>
    internal static int Side(InterfaceScale scale) => Math.Max(22, 2 * VisualSystem.Type(scale).Body);

    /// <summary>Shows or removes the badge as a replayed status changes; a source tile states readiness in its header.</summary>
    internal static void Refresh(Control card, bool exhausted)
    {
        if (card.HasMeta("source_tableau_tile")) return;
        if (exhausted)
        {
            Add(card, card.HasMeta("card_reading_scale")
                ? (InterfaceScale)card.GetMeta("card_reading_scale").AsInt32() : InterfaceScale.Percent100);
            return;
        }
        card.RemoveMeta("spatial_exhausted");
        if (card.GetNodeOrNull(NodeName) is { } badge)
        {
            card.RemoveChild(badge);
            badge.QueueFree();
        }
    }

    /// <summary>Marks a card exhausted, once.</summary>
    internal static void Add(Control card, InterfaceScale scale)
    {
        card.SetMeta("spatial_exhausted", true);
        if (card.GetNodeOrNull(NodeName) is not null) return;
        int side = Side(scale);
        var badge = new PanelContainer
        {
            Name = NodeName, TooltipText = "Exhausted", AccessibilityName = "Exhausted",
            MouseFilter = Control.MouseFilterEnum.Pass, CustomMinimumSize = new Vector2(side, side),
            Size = new Vector2(side, side), ZIndex = 2,
        };
        using var style = new StyleBoxFlat
        {
            BgColor = CardFaceStyle.Ink, BorderColor = ClientTheme.ToGodot(VisualSystem.Palette.Accent),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
        };
        badge.AddThemeStyleboxOverride("panel", style);
        float inset = side * 0.18f;
        badge.AddChild(CardGlyphRendering.Create("exhausted", "Glyph",
            new Rect2(inset, inset, side - 2 * inset, side - 2 * inset), ClientTheme.ToGodot(VisualSystem.Palette.Accent)));
        card.AddChild(badge);
        void Place()
        {
            if (!InteractionControl.IsUsable(card) || !InteractionControl.IsUsable(badge)) return;
            float width = Math.Max(card.Size.X, card.CustomMinimumSize.X);
            badge.Position = new Vector2(width - side * 0.75f, -side * 0.25f);
        }
        card.Resized += Place;
        Place();
    }
}
