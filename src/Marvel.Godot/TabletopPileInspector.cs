using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Presents one pile card at a time without displacing the stable table.</summary>
internal static class TabletopPileInspector
{
    private static PopupPanel? active;
    private static BoardRenderResult? owner;
    private static int? initialSelection;

    internal static void Show(
        Control source,
        TabletopAreaObject pile,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art,
        int initialIndex = 0)
    {
        Close();
        if (pile.InspectionOrder.Count == 0 || source.GetTree().Root is not { } root)
        {
            return;
        }

        var popup = new PopupPanel
        {
            Name = "PileInspector",
            Exclusive = false,
            MinSize = new Vector2I(
                VisualSystem.Card(CardDisplaySize.Full, scale).Width + 40,
                VisualSystem.Card(CardDisplaySize.Full, scale).MinimumHeight + 112),
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        active = popup;
        owner = result;
        initialSelection = result.SelectedAffordanceId;
        popup.SetMeta("restore_opener_focus", true);
        popup.PopupHide += () =>
        {
            bool restoreFocus = popup.GetMeta("restore_opener_focus").AsBool();
            Release(popup);
            Callable.From(() =>
            {
                if (result.IsCurrent?.Invoke() != true) return;
                result.RefreshInteraction();
                if (restoreFocus && InteractionControl.IsUsable(source))
                    source.GrabFocus();
            }).CallDeferred();
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(new Label
        {
            Name = "PileInspectorTitle",
            Text = TabletopAreaNames.Title(pile.Area.Title),
            ThemeTypeVariation = GodotThemeVariations.Caption,
        });
        var navigation = new HBoxContainer
        {
            Name = "PileInspectorNavigation",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        var previous = new Button { Name = "PreviousPileCard", Text = "‹", TooltipText = "Previous card" };
        var position = new Label
        {
            Name = "PileInspectorPosition",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var next = new Button { Name = "NextPileCard", Text = "›", TooltipText = "Next card" };
        navigation.AddChild(previous);
        navigation.AddChild(position);
        navigation.AddChild(next);
        var close = new Button
        {
            Name = "ClosePileInspector", Text = "Close",
            Shortcut = new Shortcut { Events = [new InputEventKey { Keycode = Key.Escape }] },
        };
        close.Pressed += popup.Hide;
        navigation.AddChild(close);
        stack.AddChild(navigation);
        var cardSlot = new CenterContainer
        {
            Name = "PileInspectorCard",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddChild(cardSlot);
        popup.AddChild(stack);
        root.AddChild(popup);

        int index = Math.Clamp(initialIndex, 0, pile.InspectionOrder.Count - 1);
        void Render()
        {
            foreach (Node child in cardSlot.GetChildren())
            {
                cardSlot.RemoveChild(child);
                child.QueueFree();
            }
            BoardCardPresentation card = pile.InspectionOrder[index];
            CardControl control = CardControl.Create(card, CardDisplaySize.Full, scale, art);
            cardSlot.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
            result.RefreshInteraction();
            position.Text = $"{index + 1} / {pile.InspectionOrder.Count} · Top first";
            previous.Disabled = index == 0;
            next.Disabled = index == pile.InspectionOrder.Count - 1;
        }
        previous.Pressed += () =>
        {
            if (index > 0)
            {
                index--;
                Render();
            }
        };
        next.Pressed += () =>
        {
            if (index < pile.InspectionOrder.Count - 1)
            {
                index++;
                Render();
            }
        };
        Render();

        Rect2 sourceRect = source.GetGlobalRect();
        popup.Position = new Vector2I(
            Mathf.RoundToInt(Math.Clamp(sourceRect.End.X + 12, 12,
                Math.Max(12, source.GetViewportRect().Size.X - popup.MinSize.X - 12))),
            72);
        popup.Popup();
        result.RefreshInteraction();
    }

    internal static void DraftChanged(BoardRenderResult result, int? selection)
    {
        if (ReferenceEquals(result, owner) && selection != initialSelection)
            Close(restoreFocus: false);
    }

    internal static void Close(bool restoreFocus = true)
    {
        if (active is not { } popup || !GodotObject.IsInstanceValid(popup))
        {
            active = null;
            owner = null;
            return;
        }
        active = null;
        owner = null;
        popup.SetMeta("restore_opener_focus", restoreFocus);
        popup.Hide();
        Release(popup);
    }

    private static void Release(PopupPanel popup)
    {
        if (ReferenceEquals(active, popup))
        {
            active = null;
            owner = null;
        }
        if (!popup.IsQueuedForDeletion()) popup.QueueFree();
    }
}
