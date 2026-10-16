using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the explicit, keyboard-contained choice among actions sharing one card.</summary>
internal static class BoardActionChoiceSurface
{
    private static CanvasLayer? active;
    private static Control? opener;
    private static Func<bool>? current;
    private static object? owner;

    internal static bool IsOwnedBy(object candidate) => active is { } layer
        && IsActive(layer) && ReferenceEquals(owner, candidate);

    private static bool IsActive(CanvasLayer layer) => ReferenceEquals(active, layer)
        && GodotObject.IsInstanceValid(layer) && !layer.IsQueuedForDeletion();

    internal static void Show(
        Control source,
        IReadOnlyList<AffordancePresentation> actions,
        Func<bool> isCurrent,
        Action<int> choose,
        bool recipientCommitments = false,
        object? choiceOwner = null)
    {
        Close();
        if (actions.Count < (recipientCommitments ? 1 : 2) || !InteractionControl.IsUsable(source))
        {
            return;
        }
        Node host = source;
        while (host.GetParent() is Control parent) host = parent;
        var layer = new CanvasLayer { Name = "CardActionLayer", Layer = 24 };
        active = layer;
        opener = FocusOpener(source);
        current = isCurrent;
        owner = choiceOwner;
        host.AddChild(layer);
        var overlay = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddBackdrop(overlay, source);
        var popup = new PanelContainer
        {
            Name = recipientCommitments ? "RecipientCommitments" : "CardActionChoices", Theme = source.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        overlay.AddChild(popup);
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(new Label
        {
            Text = actions[0].Anchor,
            ThemeTypeVariation = GodotThemeVariations.Heading,
        });
        string[] states = actions.Select(action => action.SourceState)
            .Where(state => !string.IsNullOrWhiteSpace(state)).Distinct().Cast<string>().ToArray();
        if (states.Length == 1)
            stack.AddChild(new Label { Text = states[0], ThemeTypeVariation = GodotThemeVariations.Caption });
        Button? first = null;
        Func<bool> valid = ChoiceValidity(layer, isCurrent);
        foreach (AffordancePresentation action in actions)
        {
            Button button = AddActionButton(stack, action, valid,
                choose, recipientCommitments, states.Length != 1);
            first ??= button;
        }
        popup.AddChild(stack);
        PlaceWhenSettled(source, popup, stack);
        ContainFocus(source, stack);
        FocusFirst(first, valid);
    }

    private static Control FocusOpener(Control source) =>
        source is Button ? source : source.GetViewport().GuiGetFocusOwner() ?? source;

    private static Func<bool> ChoiceValidity(CanvasLayer layer, Func<bool> isCurrent) =>
        () => IsActive(layer) && isCurrent();

    private static void PlaceWhenSettled(Control source, PanelContainer popup, VBoxContainer stack)
    {
        PlacePopup(source, popup, stack);
        Callable.From(() =>
        {
            if (InteractionControl.IsUsable(source) && InteractionControl.IsUsable(popup))
                PlacePopup(source, popup, stack);
        }).CallDeferred();
    }

    private static Button AddActionButton(VBoxContainer stack, AffordancePresentation action,
        Func<bool> isCurrent, Action<int> choose, bool recipientCommitments, bool showState)
    {
        var button = new Button
        {
            Name = $"Affordance{action.Id}",
            Text = PromptPresentation.Words(action.DisplayLabel ?? action.Label),
            AccessibilityDescription = PromptPresentation.Words(action.DisplayLabel ?? action.Label),
            CustomMinimumSize = new Vector2(250, 44),
            Alignment = HorizontalAlignment.Left,
            TextOverrunBehavior = recipientCommitments ? TextServer.OverrunBehavior.NoTrimming : TextServer.OverrunBehavior.TrimEllipsis,
            AutowrapMode = recipientCommitments ? TextServer.AutowrapMode.Word : TextServer.AutowrapMode.Off,
        };
        button.SetMeta("offered_verb", action.Verb);
        button.Pressed += () =>
        {
            if (!isCurrent()) return;
            Close();
            choose(action.Id);
        };
        stack.AddChild(button);
        DecisionCostLabel.AttachTo(button, action);
        if (!recipientCommitments)
        {
            stack.AddChild(new Label
            {
                Text = string.Join("\n", new[] { showState ? action.SourceState : null,
                    DecisionCopy.CompactActionSummary(action) }.Where(value => !string.IsNullOrWhiteSpace(value))),
                ThemeTypeVariation = GodotThemeVariations.Caption,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(250, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
        return button;
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
        owner = null;
    }
}
