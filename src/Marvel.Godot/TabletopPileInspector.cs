using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Presents one pile card at a time without displacing the stable table.</summary>
internal static class TabletopPileInspector
{
    private static PopupPanel? active;

    internal static void Show(
        Control source,
        TabletopAreaObject pile,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
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
                VisualSystem.Card(CardDisplaySize.Board, scale).Width + 40,
                VisualSystem.Card(CardDisplaySize.Board, scale).MinimumHeight + 112),
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        active = popup;
        popup.PopupHide += () => Release(popup);
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(new Label
        {
            Name = "PileInspectorTitle",
            Text = pile.Area.Title,
            ThemeTypeVariation = GodotThemeVariations.Eyebrow,
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
        stack.AddChild(navigation);
        var cardSlot = new CenterContainer
        {
            Name = "PileInspectorCard",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddChild(cardSlot);
        popup.AddChild(stack);
        root.AddChild(popup);

        int index = 0;
        void Render()
        {
            foreach (Node child in cardSlot.GetChildren())
            {
                cardSlot.RemoveChild(child);
                child.QueueFree();
            }
            BoardCardPresentation card = pile.InspectionOrder[index];
            CardControl control = CardControl.Create(card, CardDisplaySize.Board, scale, art);
            cardSlot.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
            result.RefreshInteraction();
            position.Text = $"{index + 1} / {pile.InspectionOrder.Count}  ·  TOP FIRST";
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
            Mathf.RoundToInt(sourceRect.Position.X),
            Mathf.RoundToInt(sourceRect.End.Y + 6));
        popup.Popup();
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
