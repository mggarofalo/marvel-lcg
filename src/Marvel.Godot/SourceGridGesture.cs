using Godot;

namespace Marvel.Godot;

/// <summary>Reversible presentation-only swaps between installed-source slots.</summary>
internal sealed partial class SourceGridGesture : Node
{
    private readonly Dictionary<Control, CardControl> cards = [];
    private readonly CardPointerLift lift = new();
    private GridContainer grid = null!;
    private ScrollContainer viewport = null!;
    private BoardRenderResult result = null!;
    private int seat;
    private CardControl? dragged;
    private Control? origin;
    private Control[] original = [];
    private Vector2 start;
    private bool moving;
    private bool keyboard;

    internal void Configure(GridContainer container, ScrollContainer scroll, int player, BoardRenderResult board)
    {
        grid = container;
        viewport = scroll;
        seat = player;
        result = board;
    }

    internal void Track(CardControl card, Control slot, string title)
    {
        cards.Add(slot, card);
        var grip = new Button { Name = "SourceReorderGrip", Text = "⠿", Flat = true,
            Position = new Vector2(card.Size.X - 24, 0), Size = new Vector2(24, 28),
            AccessibilityName = $"Reorder {title}", TooltipText = "Drag to swap table slots. Space picks up; arrows move; Enter places; Escape cancels.",
            MouseFilter = Control.MouseFilterEnum.Stop, ZIndex = 20 };
        grip.AddThemeFontSizeOverride("font_size", 16);
        using var empty = new StyleBoxEmpty();
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
            grip.AddThemeStyleboxOverride(state, empty);
        using var focus = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = CardFaceStyle.Paper,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1 };
        grip.AddThemeStyleboxOverride("focus", focus);
        grip.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        var overlay = new Control { Name = "SourceReorderOverlay", MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddChild(overlay);
        overlay.AddChild(grip);
        grip.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        grip.OffsetLeft = -24;
        grip.OffsetRight = 0;
        grip.OffsetTop = 0;
        grip.OffsetBottom = 28;
        grip.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press)
                Begin(card, slot, press.GlobalPosition, false);
            else if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
                Begin(card, slot, Vector2.Zero, true);
            else return;
            grip.AcceptEvent();
        };
    }

    private void Begin(CardControl card, Control slot, Vector2 point, bool usingKeyboard)
    {
        Cancel();
        dragged = card;
        origin = slot;
        start = point;
        keyboard = usingKeyboard;
        original = [.. grid.GetChildren().OfType<Control>()];
    }

    public override void _Input(InputEvent input)
    {
        if (dragged is null) return;
        if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }) Cancel();
        else if (keyboard && input is InputEventKey { Pressed: true, Echo: false } key) MoveByKey(key.Keycode);
        else if (input is InputEventMouseMotion motion) Preview(motion.GlobalPosition);
        else if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
            Complete(release.GlobalPosition);
        else return;
        GetViewport().SetInputAsHandled();
    }

    private void MoveByKey(Key key)
    {
        if (key == Key.Enter) { Complete(origin!.GetGlobalRect().GetCenter()); return; }
        int offset = key switch { Key.Left => -1, Key.Right => 1, Key.Up => -grid.Columns, Key.Down => grid.Columns, _ => 0 };
        if (offset == 0) return;
        int index = Math.Clamp(origin!.GetIndex() + offset, 0, original.Length - 1);
        SwapWith(original[index]);
        moving = true;
        viewport.EnsureControlVisible(origin);
    }

    private void Preview(Vector2 pointer)
    {
        if (dragged is null || !CardPointerGestureRouter.IsDrag(start, pointer)) return;
        if (!moving) { lift.Begin(dragged); moving = true; }
        CardPointerLift.Move(dragged, pointer);
        Control? target = SlotAt(pointer);
        if (target is null) return;
        SwapWith(target);
    }

    private void SwapWith(Control target)
    {
        if (target == origin) { RestoreOrder(); return; }
        int sourceIndex = original.ToList().IndexOf(origin!);
        int targetIndex = Array.IndexOf(original, target);
        RestoreOrder();
        grid.MoveChild(origin!, targetIndex);
        grid.MoveChild(target, sourceIndex);
    }

    private Control? SlotAt(Vector2 pointer)
    {
        if (!viewport.GetGlobalRect().HasPoint(pointer)) return null;
        Control? hit = grid.GetChildren().OfType<Control>().FirstOrDefault(slot => slot.GetGlobalRect().HasPoint(pointer));
        return hit is null ? null : original[hit.GetIndex()];
    }

    private void Complete(Vector2 pointer)
    {
        if (!moving || SlotAt(pointer) is null) { Cancel(); return; }
        SourcePresentationOrder.Store(seat, grid.GetChildren().OfType<Control>()
            .Select(slot => cards[slot].TargetId).OfType<int>());
        Finish();
    }

    private void Cancel()
    {
        if (dragged is null) return;
        RestoreOrder();
        Finish();
    }

    private void RestoreOrder()
    {
        for (int index = 0; index < original.Length; index++) grid.MoveChild(original[index], index);
    }

    private void Finish()
    {
        lift.Restore();
        if (InteractionControl.IsUsable(dragged))
        {
            dragged!.Position = Vector2.Zero;
            dragged.ZIndex = 0;
            result.UpdateRestingPose(dragged);
        }
        dragged = null;
        origin = null;
        moving = false;
        original = [];
    }

    public override void _ExitTree() => lift.Restore();
}
