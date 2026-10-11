using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Authorized temporary references occupy a seam, never another physical card slot.</summary>
internal static class CausalCardReferences
{
    internal static void Add(Control surface, Rect2 bounds, IEnumerable<BoardCardPresentation> sources,
        BoardRenderResult result, InterfaceScale scale)
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
            CardControl reference = SourceTableauTile.CreateCard(card, 168, scale);
            reference.Name = $"CausalReference{card.TargetId}";
            reference.SetMeta("causal_reference", true);
            row.AddChild(reference);
            // Inspection routes through the original descriptor. It is not registered as another action target.
            result.TrackPreviewOnly(reference, card);
        }
    }
}



