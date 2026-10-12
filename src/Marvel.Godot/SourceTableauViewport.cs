using Godot;

namespace Marvel.Godot;

/// <summary>Reserves native scrollbar width only when measured source rows exceed the viewport.</summary>
internal static class SourceTableauViewport
{
    internal static void Bind(ScrollContainer scroll, GridContainer grid)
    {
        bool pending = false;
        void Schedule()
        {
            if (pending) return;
            pending = true;
            Callable.From(() => { pending = false; Fit(scroll, grid); }).CallDeferred();
        }
        scroll.Resized += Schedule;
        grid.MinimumSizeChanged += Schedule;
        grid.Ready += Schedule;
    }

    private static void Fit(ScrollContainer scroll, GridContainer grid)
    {
        if (!InteractionControl.IsUsable(scroll) || scroll.Size.X <= 0) return;
        bool overflow = grid.GetCombinedMinimumSize().Y > scroll.Size.Y + 0.5f;
        float reserved = overflow ? scroll.GetVScrollBar().GetCombinedMinimumSize().X : 0;
        float width = Math.Max(1, scroll.Size.X - reserved);
        float tile = Math.Max(1, (width - (grid.Columns - 1) * SourceTableauLayout.Gap) / grid.Columns);
        Vector2 minimum = new(width, grid.CustomMinimumSize.Y);
        if (!grid.CustomMinimumSize.IsEqualApprox(minimum)) grid.CustomMinimumSize = minimum;
        foreach (Control slot in grid.GetChildren().OfType<Control>())
        {
            minimum = new Vector2(tile, slot.CustomMinimumSize.Y);
            if (!slot.CustomMinimumSize.IsEqualApprox(minimum)) slot.CustomMinimumSize = minimum;
            if (slot.GetChildCount() > 0 && slot.GetChild(0) is CardControl card)
            {
                minimum = new Vector2(tile, card.CustomMinimumSize.Y);
                if (!card.CustomMinimumSize.IsEqualApprox(minimum)) card.CustomMinimumSize = minimum;
                card.Size = new Vector2(tile, card.Size.Y);
            }
        }
    }
}
