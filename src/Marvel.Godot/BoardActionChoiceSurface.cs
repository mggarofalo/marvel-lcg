using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the explicit, keyboard-contained choice among actions sharing one card.</summary>
internal static class BoardActionChoiceSurface
{
    private static CanvasLayer? active;
    private static Control? opener;
    private static Func<bool>? current;

    internal static void Show(
        Control source,
        IReadOnlyList<AffordancePresentation> actions,
        Func<bool> isCurrent,
        Action<int> choose)
    {
        Close();
        if (actions.Count < 2 || !InteractionControl.IsUsable(source))
        {
            return;
        }
        Node host = source;
        while (host.GetParent() is Control parent) host = parent;
        var layer = new CanvasLayer { Name = "CardActionLayer", Layer = 24 };
        active = layer;
        opener = source.GetViewport().GuiGetFocusOwner() ?? source;
        current = isCurrent;
        host.AddChild(layer);
        var overlay = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddBackdrop(overlay, source);
        var popup = new PanelContainer
        {
            Name = "CardActionChoices", Theme = source.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        overlay.AddChild(popup);
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(new Label
        {
            Text = actions[0].Anchor,
            ThemeTypeVariation = GodotThemeVariations.Heading,
        });
        Button? first = null;
        foreach (AffordancePresentation action in actions)
        {
            var button = new Button
            {
                Name = $"Affordance{action.Id}",
                Text = PromptPresentation.Words(action.DisplayLabel ?? action.Label),
                TooltipText = PromptPresentation.Words(action.DisplayLabel ?? action.Label),
                CustomMinimumSize = new Vector2(250, 44),
                Alignment = HorizontalAlignment.Left,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            button.SetMeta("offered_verb", action.Verb);
            button.Pressed += () =>
            {
                if (isCurrent()) choose(action.Id);
                Close();
            };
            stack.AddChild(button);
            stack.AddChild(new Label
            {
                Text = DecisionCopy.ActionSummary(action),
                ThemeTypeVariation = GodotThemeVariations.Caption,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(250, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
            first ??= button;
        }
        popup.AddChild(stack);
        PlacePopup(source, popup, stack);
        ContainFocus(source, stack);
        FocusFirst(first, isCurrent);
    }

    private static void FocusFirst(Button? first, Func<bool> isCurrent)
    {
        if (first is null) return;
        Callable.From(() =>
        {
            if (isCurrent() && InteractionControl.IsUsable(first)) first.GrabFocus();
        }).CallDeferred();
    }

    private static void AddBackdrop(Control overlay, Control source)
    {
        var backdrop = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backdrop.GuiInput += input =>
        {
            if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
            source.GetViewport().SetInputAsHandled();
            Dismiss();
        };
    }

    private static void PlacePopup(Control source, PanelContainer popup, VBoxContainer stack)
    {
        Rect2 rect = source.GetGlobalRect();
        Vector2 viewport = source.GetViewportRect().Size;
        float height = stack.GetCombinedMinimumSize().Y;
        float width = Math.Min(420, viewport.X - 24);
        float y = rect.End.Y + 6 + height <= viewport.Y
            ? rect.End.Y + 6 : Math.Max(12, rect.Position.Y - height - 6);
        popup.Position = new Vector2(
            Mathf.RoundToInt(Math.Clamp(rect.Position.X, 12, Math.Max(12, viewport.X - width - 12))),
            Mathf.RoundToInt(y));
        popup.Size = new Vector2(width, height);
    }

    private static void ContainFocus(Control source, VBoxContainer stack)
    {
        Button[] buttons = [.. stack.GetChildren().OfType<Button>()];
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            button.FocusNext = buttons[(index + 1) % buttons.Length].GetPath();
            button.FocusPrevious = buttons[(index + buttons.Length - 1) % buttons.Length].GetPath();
            button.GuiInput += input => RouteInput(source.GetViewport(), input);
        }
    }

    internal static bool RouteInput(Viewport viewport, InputEvent input)
    {
        if (active is not { } layer || !GodotObject.IsInstanceValid(layer)
            || layer.IsQueuedForDeletion() || layer.GetViewport() != viewport) return false;
        if (input.IsActionPressed("ui_cancel"))
        {
            viewport.SetInputAsHandled();
            Dismiss();
        }
        return true;
    }

    private static void Dismiss()
    {
        Control? prior = opener;
        bool restore = current?.Invoke() == true;
        Close();
        Callable.From(() =>
        {
            if (active is null && restore && InteractionControl.IsUsable(prior)
                && prior!.IsVisibleInTree()) prior.GrabFocus();
        }).CallDeferred();
    }

    internal static void Close()
    {
        if (active is { } layer && GodotObject.IsInstanceValid(layer) && !layer.IsQueuedForDeletion())
            layer.QueueFree();
        active = null;
        opener = null;
        current = null;
    }
}
