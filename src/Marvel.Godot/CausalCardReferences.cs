using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Authorized temporary references occupy a seam, never another physical card slot.</summary>
internal static class CausalCardReferences
{
    internal static void Add(Control surface, Rect2 bounds, IEnumerable<BoardCardPresentation> sources,
        BoardRenderResult result, InterfaceScale scale, bool seam = false)
    {
        BoardCardPresentation[] cards = [.. sources.Where(card => !card.Concealed).DistinctBy(card => card.TargetId)];
        if (cards.Length == 0) return;
        var row = new VBoxContainer { Name = "CausalReferences", Position = bounds.Position,
            Size = new Vector2(168, bounds.Size.Y), MouseFilter = Control.MouseFilterEnum.Ignore };
        var scroll = new ScrollContainer { Name = "CausalReferenceStack", Position = bounds.Position,
            Size = new Vector2(168, bounds.Size.Y), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true };
        row.Position = Vector2.Zero;
        scroll.AddChild(row);
        surface.AddChild(scroll);

        result.Inspector.Register(cards);
        foreach (BoardCardPresentation card in cards)
        {
            CardControl reference = CausalReferenceTile.Create(card, 168, scale);
            reference.Name = $"CausalReference{card.TargetId}";
            reference.SetMeta("causal_reference", true);
            if (seam) SeamStyle(reference);
            row.AddChild(reference);
            // Inspection routes through the original descriptor. It is not registered as another action target.
            result.TrackPreviewOnly(reference, card);
        }
    }

    // A column table's seam is a thin band between the mat and the tableau. A reference there
    // reads as part of the seam's chrome rather than a sheet of card paper; the choice is ours.
    private static void SeamStyle(CardControl reference)
    {
        using var style = new StyleBoxFlat { BgColor = ClientTheme.ToGodot(VisualSystem.Palette.RaisedSurface),
            BorderColor = ClientTheme.ToGodot(VisualSystem.Palette.Outline), BorderWidthLeft = 3,
            ContentMarginLeft = 8, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4 };
        reference.AddThemeStyleboxOverride("panel", style);
        reference.SetMeta("seam_reference", true);
        if (reference.FindChild("ReferenceTitle", true, false) is Label title)
            title.AddThemeColorOverride("font_color", ClientTheme.ToGodot(VisualSystem.Palette.Text));
    }
}



