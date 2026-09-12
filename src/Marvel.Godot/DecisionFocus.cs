using Godot;

namespace Marvel.Godot;

/// <summary>Restores decision focus and keeps nested decision scrolling readable.</summary>
internal static class DecisionFocus
{
    internal static void Restore(DecisionPanel panel, string? requested, bool focusFirst)
    {
        Control? candidate = EnabledControl(panel, requested);
        if (candidate is null && requested is not null)
        {
            candidate = EnabledButton(panel, PairedControlName(requested))
                ?? EnabledButton(panel, "Submit");
        }
        candidate ??= FirstEnabledButton(panel, focusFirst || requested is not null);
        if (candidate is not null)
        {
            FocusWhenLayoutSettles(candidate);
        }
    }

    internal static string? Key(DecisionPanel panel, Control focused)
    {
        Node? current = focused;
        while (current is not null && current != panel)
        {
            string name = current.Name.ToString();
            if (IsStableName(name))
            {
                return name;
            }
            current = current.GetParent();
        }
        return null;
    }

    private static string? PairedControlName(string requested) =>
        requested.EndsWith("Add", StringComparison.Ordinal)
            ? requested[..^3] + "Remove"
            : requested.EndsWith("Remove", StringComparison.Ordinal)
                ? requested[..^6] + "Add"
                : null;

    private static BaseButton? FirstEnabledButton(DecisionPanel panel, bool search) =>
        search
            ? panel.FindChildren("*", "BaseButton", recursive: true, owned: false)
                .OfType<BaseButton>()
                .FirstOrDefault(button => !button.Disabled)
            : null;

    private static void FocusWhenLayoutSettles(Control candidate)
    {
        candidate.GrabFocus();
        Callable.From(() =>
        {
            EnsureVisible(candidate);
            // Nested scroll containers settle from the decision rail out to the page.
            Callable.From(() => EnsureVisible(candidate)).CallDeferred();
        }).CallDeferred();
    }

    private static void EnsureVisible(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor is ScrollContainer scroll)
            {
                EnsureVisibleWithin(scroll, control);
            }
            ancestor = ancestor.GetParent();
        }
    }

    private static void EnsureVisibleWithin(ScrollContainer scroll, Control control)
    {
        if (scroll.Name == "Margin")
        {
            if (scroll.VerticalScrollMode == ScrollContainer.ScrollMode.Disabled)
            {
                scroll.ScrollVertical = 0;
            }
            else
            {
                EnsurePromptContextVisible(scroll, control);
            }
            return;
        }
        scroll.EnsureControlVisible(control);
        if (scroll.Name == "DecisionBodyScroll")
        {
            scroll.ScrollHorizontal = 0;
        }
    }

    private static void EnsurePromptContextVisible(ScrollContainer page, Control control)
    {
        Control? header = PromptHeader(control);
        if (header is null)
        {
            page.EnsureControlVisible(control);
            return;
        }
        Rect2 viewport = page.GetGlobalRect();
        Rect2 headerRect = header.GetGlobalRect();
        Rect2 controlRect = control.GetGlobalRect();
        float top = Math.Min(headerRect.Position.Y, controlRect.Position.Y);
        float bottom = Math.Max(headerRect.End.Y, controlRect.End.Y);
        if (bottom - top > viewport.Size.Y)
        {
            page.EnsureControlVisible(control);
            return;
        }
        page.ScrollVertical += Mathf.RoundToInt(top - viewport.Position.Y);
    }

    private static Control? PromptHeader(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor.Name == "Stack")
            {
                return ancestor.GetNodeOrNull<Control>("PromptHeader");
            }
            ancestor = ancestor.GetParent();
        }
        return null;
    }

    private static bool IsStableName(string name) =>
        name is "Submit" or "Decline"
        || name.StartsWith("Affordance", StringComparison.Ordinal)
        || name.StartsWith("Group", StringComparison.Ordinal)
        || name.StartsWith("Target", StringComparison.Ordinal)
        || name.StartsWith("Cost", StringComparison.Ordinal)
        || name.StartsWith("Variable", StringComparison.Ordinal)
        || name.StartsWith("Resource", StringComparison.Ordinal)
        || name.StartsWith("Allocation", StringComparison.Ordinal);

    private static Control? EnabledControl(DecisionPanel panel, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        return panel.FindChild(name, recursive: true, owned: false) switch
        {
            BaseButton { Disabled: false } button => button,
            SpinBox { Editable: true } spin => spin.GetLineEdit(),
            LineEdit { Editable: true } line => line,
            _ => null,
        };
    }

    private static BaseButton? EnabledButton(DecisionPanel panel, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        return panel.FindChild(name, recursive: true, owned: false) is BaseButton
        {
            Disabled: false,
        } button
            ? button
            : null;
    }
}
