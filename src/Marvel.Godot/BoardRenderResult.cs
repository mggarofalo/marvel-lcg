using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    private readonly Dictionary<Control, Action> areaExpanders = [];

    /// <summary>Identifies whether this render remains the board currently shown by its owner.</summary>
    internal Func<bool>? IsCurrent { get; set; }

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
            ? matches.LastOrDefault(InteractionControl.IsUsable)
            : null;

    /// <summary>Highlights every visible control matching server-provided ids.</summary>
    public void Highlight(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> highlighted = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                match.SetHighlighted(highlighted.Contains(key));
                if (highlighted.Contains(key))
                {
                    EnsureVisible(key);
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
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                match.SetPresented(presented.Contains(key));
                if (presented.Contains(key))
                {
                    EnsureVisible(key);
                }
            }
        }
    }

    private void EnsureVisible(int key)
    {
        if (!TryCurrentControl(key, out Control? control))
        {
            return;
        }

        RevealAncestors(key, control!);
    }

    private void RevealAncestors(int key, Control control)
    {
        for (Node? ancestor = control.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is Control areaBody)
            {
                ExpandArea(areaBody);
            }
            if (ancestor is ScrollContainer scroll)
            {
                if (!CanReveal(scroll, control))
                {
                    return;
                }

                RevealInScroll(key, scroll.Name);
                // The board owns card navigation. Continuing into the outer
                // page would hide the table heading whenever prompt focus
                // highlights a card below the fold.
                if (scroll.Name == "TableScroll")
                {
                    break;
                }
            }

        }
    }

    private static bool CanReveal(ScrollContainer scroll, Control control) =>
        InteractionControl.IsUsable(scroll) && scroll.IsAncestorOf(control);

    private void ExpandArea(Control areaBody)
    {
        if (areaExpanders.TryGetValue(areaBody, out Action? expand))
        {
            expand();
        }
    }

    private void RevealInScroll(int key, StringName scrollName)
    {
        if (!TryFindScrollControl(key, scrollName, out ScrollContainer? scroll, out Control? control))
        {
            return;
        }

        if (scroll!.Name == "TableScroll")
        {
            RevealTableWhenSettled(key, scrollName);
            return;
        }

        RevealAreaWhenSettled(key, scrollName);
    }

    private void RevealAreaWhenSettled(int key, StringName scrollName)
    {
        Callable.From(() =>
        {
            if (TryFindScrollControl(key, scrollName, out ScrollContainer? current, out Control? card))
            {
                ScrollContainer settledScroll = current!;
                Control settledCard = card!;
                settledScroll.EnsureControlVisible(
                    AreaContaining(settledScroll, settledCard) ?? settledCard);
            }
        }).CallDeferred();
    }

    private void RevealTableWhenSettled(int key, StringName scrollName)
    {
        Callable.From(() =>
        {
            if (!TryFindScrollControl(key, scrollName, out ScrollContainer? current, out Control? card))
            {
                return;
            }
            Control settledCard = card!;
            Control title = settledCard.GetNodeOrNull<Control>("CardFace/Title") ?? settledCard;
            current!.EnsureControlVisible(title);
            Callable.From(() => AlignBoardToCardFrame(key, scrollName, 3)).CallDeferred();
        }).CallDeferred();
    }

    private void AlignBoardToCardFrame(
        int key,
        StringName scrollName,
        int remainingPasses)
    {
        if (!TryFindScrollControl(key, scrollName, out ScrollContainer? board, out Control? resolved))
        {
            return;
        }

        ScrollContainer currentBoard = board!;
        Control currentControl = resolved!;
        currentBoard.ScrollVertical = Math.Clamp(
            currentBoard.ScrollVertical + FrameAlignmentDelta(currentBoard, currentControl),
            0,
            ScrollMaximum(currentBoard));
        ScheduleAlignment(key, scrollName, remainingPasses);
    }

    private static int ScrollMaximum(ScrollContainer board)
    {
        VScrollBar bar = board.GetVScrollBar();
        return Math.Max(0, Mathf.CeilToInt((float)(bar.MaxValue - bar.Page)));
    }

    private static int FrameAlignmentDelta(ScrollContainer board, Control resolved)
    {
        Control title = resolved.GetNodeOrNull<Control>("CardFace/Title") ?? resolved;
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
        return Mathf.RoundToInt(delta);
    }

    private void ScheduleAlignment(int key, StringName scrollName, int remainingPasses)
    {
        if (remainingPasses > 0)
        {
            Callable.From(() => AlignBoardToCardFrame(key, scrollName, remainingPasses - 1))
                .CallDeferred();
        }
    }

    private bool TryCurrentControl(int key, out Control? control)
    {
        control = IsCurrent?.Invoke() == true ? ControlFor(key) : null;
        return control is not null;
    }

    private bool TryFindScrollControl(
        int key,
        StringName scrollName,
        out ScrollContainer? scroll,
        out Control? control)
    {
        scroll = null;
        control = null;
        if (!TryCurrentControl(key, out Control? found))
        {
            return false;
        }

        ScrollContainer? containing = InteractionControl.ScrollAncestor(found!, scrollName.ToString());
        if (containing is null)
        {
            return false;
        }

        scroll = containing;
        control = found;
        return true;
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
