using Godot;

namespace Marvel.Godot;

/// <summary>Reveals a current board card after layout settles without retaining discarded controls.</summary>
internal sealed class BoardControlReveal
{
    private readonly BoardRenderResult result;

    internal BoardControlReveal(BoardRenderResult result)
    {
        this.result = result;
    }

    internal void Ensure(int key)
    {
        if (!result.TryCurrentControl(key, out Control? control))
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
                result.ExpandArea(areaBody);
            }
            if (ancestor is ScrollContainer scroll)
            {
                if (!InteractionControl.IsUsable(scroll) || !scroll.IsAncestorOf(control))
                {
                    return;
                }

                RevealInScroll(key, scroll.Name);
                if (scroll.Name == "TableScroll")
                {
                    break;
                }
            }
        }
    }

    private void RevealInScroll(int key, StringName scrollName)
    {
        if (!TryFindScrollControl(key, scrollName, out ScrollContainer? scroll, out _))
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
            if (TryFindScrollControl(key, scrollName, out ScrollContainer? scroll, out Control? card))
            {
                scroll!.EnsureControlVisible(AreaContaining(scroll, card!) ?? card!);
            }
        }).CallDeferred();
    }

    private void RevealTableWhenSettled(int key, StringName scrollName)
    {
        Callable.From(() =>
        {
            if (!TryFindScrollControl(key, scrollName, out ScrollContainer? scroll, out Control? card))
            {
                return;
            }

            Control settled = card!;
            scroll!.EnsureControlVisible(settled.GetNodeOrNull<Control>("CardFace/Title") ?? settled);
            Callable.From(() => AlignBoardToCardFrame(key, scrollName, 3)).CallDeferred();
        }).CallDeferred();
    }

    private void AlignBoardToCardFrame(int key, StringName scrollName, int passes)
    {
        if (!TryFindScrollControl(key, scrollName, out ScrollContainer? board, out Control? card))
        {
            return;
        }

        board!.ScrollVertical = Math.Clamp(
            board.ScrollVertical + FrameAlignmentDelta(board, card!), 0, ScrollMaximum(board));
        if (passes > 0)
        {
            Callable.From(() => AlignBoardToCardFrame(key, scrollName, passes - 1)).CallDeferred();
        }
    }

    private bool TryFindScrollControl(
        int key,
        StringName scrollName,
        out ScrollContainer? scroll,
        out Control? control)
    {
        scroll = null;
        control = null;
        if (!result.TryCurrentControl(key, out Control? found))
        {
            return false;
        }

        scroll = InteractionControl.ScrollAncestor(found!, scrollName.ToString());
        if (scroll is null)
        {
            return false;
        }

        control = found;
        return true;
    }

    private static int ScrollMaximum(ScrollContainer board)
    {
        VScrollBar bar = board.GetVScrollBar();
        return Math.Max(0, Mathf.CeilToInt((float)(bar.MaxValue - bar.Page)));
    }

    private static int FrameAlignmentDelta(ScrollContainer board, Control control)
    {
        Control title = control.GetNodeOrNull<Control>("CardFace/Title") ?? control;
        Control card = CardContaining(board, title) ?? title;
        Control? area = AreaContaining(board, card);
        Control? disclosure = area is null ? null : DisclosureIn(area);
        Rect2 viewport = board.GetGlobalRect();
        Rect2 cardRect = card.GetGlobalRect();
        float top = (disclosure?.GetGlobalRect().Position.Y ?? cardRect.Position.Y)
            - (viewport.Position.Y + 12);
        float bottom = cardRect.End.Y - (viewport.End.Y - 12);
        return Mathf.RoundToInt(bottom <= top ? top : bottom);
    }

    private static CardControl? CardContaining(ScrollContainer scroll, Control control)
    {
        for (Node? candidate = control; candidate is not null && candidate != scroll;
             candidate = candidate.GetParent())
        {
            if (candidate is CardControl card)
            {
                return card;
            }
        }

        return null;
    }

    private static Button? DisclosureIn(Control area) => area.GetChildren()
        .SelectMany(child => child.GetChildren())
        .OfType<Button>()
        .FirstOrDefault(button => button.Name.ToString().StartsWith("Area", StringComparison.Ordinal)
            && button.Name.ToString().EndsWith("Disclosure", StringComparison.Ordinal));

    private static Control? AreaContaining(ScrollContainer scroll, Control control)
    {
        Control? area = null;
        for (Node? candidate = control; candidate is not null && candidate != scroll;
             candidate = candidate.GetParent())
        {
            if (candidate is PanelContainer panel
                && panel.Name.ToString().StartsWith("Area", StringComparison.Ordinal))
            {
                area = panel;
            }
        }

        return area;
    }
}
