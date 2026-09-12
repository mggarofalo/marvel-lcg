using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    private readonly Dictionary<Control, Action> areaExpanders = [];

    /// <summary>Raised with the card under the pointer, or null when it leaves.</summary>
    public event Action<BoardCardPresentation, Control>? CardActivated;

    internal void Register(int id, CardControl control)
    {
        if (!controls.TryGetValue(id, out List<CardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    internal void RegisterArea(Control body, Action expand) =>
        areaExpanders.Add(body, expand);

    internal void TrackCard(Control control, BoardCardPresentation card)
    {
        control.GuiInput += input =>
        {
            bool pointer = input is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            };
            bool keyboard = input is InputEventKey { Echo: false }
                && input.IsActionPressed("ui_accept");
            if (pointer || keyboard)
            {
                CardActivated?.Invoke(card, control);
                control.AcceptEvent();
            }
        };
    }

    /// <summary>Returns the visible control for an engine-provided card id.</summary>
    public Control? ControlFor(int id) =>
        controls.TryGetValue(id, out List<CardControl>? matches)
            ? matches.LastOrDefault()
            : null;

    /// <summary>Highlights every visible control matching server-provided ids.</summary>
    public void Highlight(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> highlighted = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetHighlighted(highlighted.Contains(key));
                if (highlighted.Contains(key))
                {
                    EnsureVisible(match);
                }
            }
        }
    }

    /// <summary>Marks cards for a transient event cue without disturbing prompt focus.</summary>
    public void Present(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> presented = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetPresented(presented.Contains(key));
                if (presented.Contains(key))
                {
                    EnsureVisible(match);
                }
            }
        }
    }

    private void EnsureVisible(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor is Control areaBody)
            {
                ExpandArea(areaBody);
            }
            if (ancestor is ScrollContainer scroll)
            {
                RevealInScroll(scroll, control);
                // The board owns card navigation. Continuing into the outer
                // page would hide the table heading whenever prompt focus
                // highlights a card below the fold.
                if (scroll.Name == "TableScroll")
                {
                    break;
                }
            }

            ancestor = ancestor.GetParent();
        }
    }

    private void ExpandArea(Control areaBody)
    {
        if (areaExpanders.TryGetValue(areaBody, out Action? expand))
        {
            expand();
        }
    }

    private static void RevealInScroll(ScrollContainer scroll, Control control)
    {
        bool table = scroll.Name == "TableScroll";
        Control target = table
            ? control.GetNodeOrNull<Control>("CardFace/Title") ?? control
            : AreaContaining(scroll, control) ?? control;
        if (!table)
        {
            scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible, target);
            return;
        }
        Callable.From(() =>
        {
            scroll.EnsureControlVisible(target);
            Callable.From(() => AlignBoardToCardFrame(scroll, target, 3)).CallDeferred();
        }).CallDeferred();
    }

    private static void AlignBoardToCardFrame(
        ScrollContainer board,
        Control title,
        int remainingPasses)
    {
        const int topInset = 12;
        Rect2 viewport = board.GetGlobalRect();
        Control card = CardContaining(board, title) ?? title;
        Control? area = AreaContaining(board, card);
        Control? disclosure = area is null ? null : DisclosureIn(area);
        Rect2 cardRect = card.GetGlobalRect();
        float contentTop = disclosure?.GetGlobalRect().Position.Y ?? cardRect.Position.Y;
        float alignTop = contentTop - (viewport.Position.Y + topInset);
        float revealBottom = cardRect.End.Y - (viewport.End.Y - topInset);
        float delta = revealBottom <= alignTop ? alignTop : revealBottom;
        VScrollBar bar = board.GetVScrollBar();
        int maximum = Math.Max(0, Mathf.CeilToInt((float)(bar.MaxValue - bar.Page)));
        board.ScrollVertical = Math.Clamp(
            board.ScrollVertical + Mathf.RoundToInt(delta),
            0,
            maximum);
        if (remainingPasses > 0)
        {
            Callable.From(() => AlignBoardToCardFrame(board, title, remainingPasses - 1))
                .CallDeferred();
        }
    }

    private static CardControl? CardContaining(ScrollContainer scroll, Control control)
    {
        Node? candidate = control;
        while (candidate is not null && candidate != scroll)
        {
            if (candidate is CardControl card)
            {
                return card;
            }
            candidate = candidate.GetParent();
        }
        return null;
    }

    private static Button? DisclosureIn(Control area)
    {
        foreach (Node child in area.GetChildren())
        {
            foreach (Node candidate in child.GetChildren())
            {
                if (candidate is Button button
                    && button.Name.ToString().StartsWith("Area", StringComparison.Ordinal)
                    && button.Name.ToString().EndsWith("Disclosure", StringComparison.Ordinal))
                {
                    return button;
                }
            }
        }
        return null;
    }

    private static Control? AreaContaining(ScrollContainer scroll, Control control)
    {
        Node? candidate = control;
        Control? area = null;
        while (candidate is not null && candidate != scroll)
        {
            if (candidate is PanelContainer panel
                && panel.Name.ToString().StartsWith("Area", StringComparison.Ordinal))
            {
                area = panel;
            }

            candidate = candidate.GetParent();
        }

        return area;
    }
}
