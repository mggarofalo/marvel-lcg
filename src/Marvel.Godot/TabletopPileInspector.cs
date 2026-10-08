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
        int initialIndex = 0,
        bool allowActions = true)
    {
        Close(restoreFocus: false);
        if (pile.InspectionOrder.Count == 0 || source.GetTree().Root is not { } root)
        {
            return;
        }

        var popup = new PopupPanel
        {
            Name = "PileInspector",
            Exclusive = false,
            Theme = ClientTheme.Create(scale),
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        active = popup;
        owner = result;
        initialSelection = result.SelectedAffordanceId;
        BindDismissal(popup, source, result);
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
        var previous = new Button { Name = "PreviousPileCard", Text = "‹", TooltipText = "Previous card", CustomMinimumSize = new Vector2(44, 44) };
        var position = new Label
        {
            Name = "PileInspectorPosition",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var next = new Button { Name = "NextPileCard", Text = "›", TooltipText = "Next card", CustomMinimumSize = new Vector2(44, 44) };
        navigation.AddChild(previous);
        navigation.AddChild(position);
        navigation.AddChild(next);
        var close = new Button
        {
            Name = "ClosePileInspector", Text = "Close", CustomMinimumSize = new Vector2(44, 44),
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
            InterfaceScale fitted = CardInspectorFocus.FittedScale(card, scale, source.GetViewportRect().Size.Y - 96);
            CardInspectionContent detail = CardInspectionContent.Create(card, fitted, art, beside: true,
                inspect: valueSource => OpenSource(source, card, valueSource, result, scale, art));
            CardControl control = detail.Face;
            cardSlot.AddChild(detail.Body);
            if (allowActions && card.TargetId is { } target)
            {
                result.Register(target, control);
                result.TrackInspection(control, card);
            }
            result.RefreshInteraction();
            position.Text = $"{index + 1} / {pile.InspectionOrder.Count}" + (pile.IsPile ? " · Top first" : "");
            previous.Disabled = index == 0;
            next.Disabled = index == pile.InspectionOrder.Count - 1;
            detail.Body.MinimumSizeChanged += () => Callable.From(() => Fit(popup, stack, source)).CallDeferred();
            Callable.From(() => Fit(popup, stack, source)).CallDeferred();
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

        popup.Popup();
        result.RefreshInteraction();
    }

    private static void BindDismissal(PopupPanel popup, Control source, BoardRenderResult result)
    {
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
                    CardFocusPreview.Restore(source);
            }).CallDeferred();
        };
    }

    private static void Fit(PopupPanel popup, Control content, Control source)
    {
        if (!GodotObject.IsInstanceValid(popup) || popup.IsQueuedForDeletion()
            || !InteractionControl.IsUsable(source)) return;
        Rect2 frame = CardInspectorPlacement.Fit(source.GetViewportRect().Size,
            SpatialCardFootprint.Face(source), content.GetCombinedMinimumSize() + new Vector2(20, 20));
        popup.MinSize = new Vector2I(Mathf.CeilToInt(frame.Size.X), Mathf.CeilToInt(frame.Size.Y));
        popup.Size = popup.MinSize;
        popup.Position = new Vector2I(Mathf.RoundToInt(frame.Position.X), Mathf.RoundToInt(frame.Position.Y));
    }

    private static void OpenSource(Control opener, BoardCardPresentation host, BoardCardPresentation source,
        BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
    {
        if (result.IsCurrent?.Invoke() != true) return;
        BoardCardPresentation detail = result.Inspector.Source(source);
        var area = new BoardAreaPresentation(-1, "Card details", "", [host, detail], []);
        Show(opener, TabletopAreaObject.From(area), result, scale, art, allowActions: false);
    }

    internal static void DraftChanged(BoardRenderResult result, int? selection)
    {
        if (ReferenceEquals(result, owner) && selection != initialSelection)
            Close(restoreFocus: false);
    }

    internal static void CloseFor(BoardRenderResult result)
    {
        if (ReferenceEquals(result, owner)) Close(restoreFocus: false);
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
