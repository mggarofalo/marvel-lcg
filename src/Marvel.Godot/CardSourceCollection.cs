using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A bounded source viewport. The title picker changes the inspected physical source, never a draft.</summary>
internal static class CardSourceCollection
{
    internal static VBoxContainer Create(BoardCardPresentation[] cards, float width,
        BoardRenderResult result, InterfaceScale scale, string relationship)
    {
        var stack = new VBoxContainer { Name = "SourceCollection", CustomMinimumSize = new Vector2(width, 0),
            Size = new Vector2(width, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        stack.AddThemeConstantOverride("separation", 4);
        var picker = new MenuButton { Name = "SourcePicker", Text = $"{relationship} · {cards.Length} ▾",
            CustomMinimumSize = new Vector2(width, 44), FocusMode = Control.FocusModeEnum.All };
        picker.AddThemeFontSizeOverride("font_size", 12);
        stack.AddChild(picker);
        for (int index = 0; index < cards.Length; index++)
            picker.GetPopup().AddItem($"{index + 1} · {CardStatePresentation.Summary(cards[index])}", index);
        result.Inspector.Register(cards);
        CardControl current = Show(stack, cards[0], width, result, scale);
        picker.GetPopup().IdPressed += id =>
        {
            if (result.IsCurrent?.Invoke() != true || !InteractionControl.IsUsable(stack)) return;
            stack.RemoveChild(current);
            current.QueueFree();
            current = Show(stack, cards[(int)id], width, result, scale);
            result.RefreshInteraction();
            picker.GrabFocus();
        };
        return stack;
    }

    private static CardControl Show(VBoxContainer parent, BoardCardPresentation card, float width,
        BoardRenderResult result, InterfaceScale scale)
    {
        CardControl control = CardControl.CreateSource(card, width, scale);
        control.Name = $"ProceduralCard{card.TargetId}";
        parent.AddChild(control);
        if (card.TargetId is { } id) result.Register(id, control);
        result.TrackCard(control, card);
        parent.SortChildren += () => Callable.From(() =>
        { if (InteractionControl.IsUsable(control)) result.UpdateRestingPose(control); }).CallDeferred();
        return control;
    }
}
