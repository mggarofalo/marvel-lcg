using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the explicit, keyboard-contained choice among actions sharing one card.</summary>
internal static class BoardActionChoiceSurface
{
    private static PopupPanel? active;

    internal static void Show(
        Control source,
        IReadOnlyList<AffordancePresentation> actions,
        Func<bool> isCurrent,
        Action<int> choose)
    {
        Close();
        if (actions.Count < 2 || source.GetTree().Root is not { } root)
        {
            return;
        }
        var popup = new PopupPanel
        {
            Name = "CardActionChoices",
            Exclusive = true,
            MinSize = new Vector2I(260, 0),
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        active = popup;
        popup.PopupHide += () => Release(popup);
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(new Label
        {
            Text = "CHOOSE ACTION",
            ThemeTypeVariation = GodotThemeVariations.Eyebrow,
        });
        Button? first = null;
        foreach (AffordancePresentation action in actions)
        {
            var button = new Button
            {
                Name = $"Affordance{action.Id}",
                Text = $"◇ {action.Verb}  ·  {action.Label}",
                Alignment = HorizontalAlignment.Left,
            };
            button.Pressed += () =>
            {
                if (isCurrent()) choose(action.Id);
                Close();
            };
            stack.AddChild(button);
            first ??= button;
        }
        popup.AddChild(stack);
        root.AddChild(popup);
        Rect2 rect = source.GetGlobalRect();
        popup.Position = new Vector2I(Mathf.RoundToInt(rect.Position.X),
            Mathf.RoundToInt(rect.End.Y + 6));
        popup.Popup();
        if (first is not null)
        {
            Callable.From(first.GrabFocus).CallDeferred();
        }
    }

    internal static void Close()
    {
        if (active is not { } popup || !GodotObject.IsInstanceValid(popup))
        {
            active = null;
            return;
        }
        active = null;
        popup.Hide();
        Release(popup);
    }

    private static void Release(PopupPanel popup)
    {
        if (ReferenceEquals(active, popup)) active = null;
        if (!popup.IsQueuedForDeletion()) popup.QueueFree();
    }
}
