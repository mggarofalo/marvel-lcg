using Godot;

namespace Marvel.Godot;

/// <summary>Reversible presentation-only placement into occupied or vacant installed-source slots.</summary>
internal sealed partial class SourceGridGesture : Node
{
    private readonly Dictionary<Control, CardControl> cards = [];
    private readonly List<Control> vacancies = [];
    private readonly CardPointerLift lift = new();
    private readonly CardReturnMotion returns = new();
    private int? hoveredSlot;
    private readonly DragPointerFeedback pointerFeedback = new();
    private SourceGridSlotMotion slotMotion = null!;
    private Control? swapped;
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
    private Button? handle;
    private Button? clickedAction;
    private SourceArrangementFeedback feedback = null!;

    internal void Configure(GridContainer container, ScrollContainer scroll, int player, BoardRenderResult board)
    {
        grid = container;
        viewport = scroll;
        seat = player;
        result = board;
        slotMotion = new SourceGridSlotMotion(grid, cards, () => result.MotionEnabled);
        feedback = new SourceArrangementFeedback(scroll.GetParent<Control>());
    }

    internal void TrackEmpty(Control slot)
    {
        vacancies.Add(slot);
        feedback.Track(slot);
        slot.Draw += () =>
        {
            if (dragged is null) return;
            slot.DrawRect(new Rect2(Vector2.One * 2, slot.Size - Vector2.One * 4),
                CardFaceStyle.Paper with { A = 0.3f }, false, 1);
        };
    }

    internal void Track(CardControl card, Control slot)
    {
        cards.Add(slot, card);
        feedback.Track(slot);
        string identity = card.GetMeta("source_identity").AsString();
        Control header = card.FindChild("SourceTitleBand", true, false) as Control ?? card;
        Label title = (Label)card.FindChild("SourceTitle", true, false);
        var grip = new Button { Name = "SourceReorderGrip", Text = "", Flat = true,
            AccessibilityName = $"Arrange {identity}", TooltipText = "Drag and drop into any slot. Space picks up; arrows move; Enter places; Escape cancels.",
            Alignment = HorizontalAlignment.Right, MouseDefaultCursorShape = Control.CursorShape.Drag,
            MouseFilter = Control.MouseFilterEnum.Stop, ZIndex = 20 };
        grip.AddThemeFontSizeOverride("font_size", 16);
        using var empty = new StyleBoxEmpty();
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
            grip.AddThemeStyleboxOverride(state, empty);
        using var focus = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = CardFaceStyle.Paper,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1 };
        grip.AddThemeStyleboxOverride("focus", focus);
        grip.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        grip.AddThemeColorOverride("font_hover_color", CardFaceStyle.Paper);
        grip.AddThemeColorOverride("font_pressed_color", CardFaceStyle.Paper);
        var overlay = new Control { Name = "SourceReorderOverlay", MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddChild(overlay);
        overlay.AddChild(grip);
        void Fit()
        {
            grip.Position = header.Position;
            grip.Size = new Vector2(header.Size.X, title.Size.Y);
        }
        header.Resized += Fit;
        title.Resized += Fit;
        card.Ready += Fit;
        grip.FocusExited += () => { if (keyboard && handle == grip) Cancel(); };
        grip.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press)
                Begin(card, slot, grip, press.GlobalPosition, false);
            else if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
                Begin(card, slot, grip, Vector2.Zero, true);
            else return;
            grip.AcceptEvent();
        };
        BindBody(card, card, slot, grip);
        card.GetNode("SourceBody/DirectControls").ChildEnteredTree += child =>
        {
            if (child is Button action) BindAction(action, card, slot, grip);
        };
    }

    private void BindAction(Button action, CardControl card, Control slot, Button grip)
    {
        action.GuiInput += input =>
        {
            if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press)
                return;
            Begin(card, slot, grip, press.GlobalPosition, false);
            clickedAction = action;
            action.AcceptEvent();
        };
    }

    private void BindBody(Node node, CardControl card, Control slot, Button grip)
    {
        if (node is BaseButton || node.Name == "SourceReorderOverlay") return;
        if (node is Control body)
            body.GuiInput += input =>
            {
                if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press)
                    return;
                Begin(card, slot, grip, press.GlobalPosition, false);
                body.AcceptEvent();
            };
        foreach (Node child in node.GetChildren()) BindBody(child, card, slot, grip);
    }

    private void Begin(CardControl card, Control slot, Button grip, Vector2 point, bool usingKeyboard)
    {
        Cancel();
        returns.Stop(card);
        dragged = card;
        origin = slot;
        handle = grip;
        start = point;
        keyboard = usingKeyboard;
        original = [.. grid.GetChildren().OfType<Control>()];
        result.SetSourceArrangement(true);
        foreach (Control vacancy in vacancies) vacancy.QueueRedraw();
        feedback.Show(slot, $"Picked up {Identity(slot)} · {Place(slot)}. Arrows move; Enter places; Escape cancels.");
    }

    public override void _Input(InputEvent input)
    {
        if (dragged is null) return;
        if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }) Cancel();
        else if (keyboard && input is InputEventKey { Pressed: true, Echo: false } key) MoveByKey(key.Keycode);
        else if (!keyboard && input is InputEventMouseMotion motion) Preview(motion.GlobalPosition);
        else if (!keyboard && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
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
        if (!moving) { lift.Begin(dragged, start); pointerFeedback.Begin(dragged, Cancel); moving = true; }
        lift.Move(dragged, pointer);
        Control? target = SlotAt(pointer);
        pointerFeedback.Preview(pointer, target is not null, "Outside source grid · release to cancel");
        if (target is null)
        {
            feedback.Show(origin!, "Outside the source grid · release to cancel. Your table order is unchanged.");
            return;
        }
        SwapWith(target);
    }

    private void SwapWith(Control target)
    {
        if (swapped == target) return;
        swapped = target;
        slotMotion.Capture(dragged);
        if (target == origin)
        {
            RestoreOrder();
            feedback.Show(origin!, $"Return {Identity(origin!)} to {Place(origin!)} · Escape cancels.");
            return;
        }
        int sourceIndex = original.ToList().IndexOf(origin!);
        int targetIndex = Array.IndexOf(original, target);
        RestoreOrder();
        grid.MoveChild(origin!, targetIndex);
        grid.MoveChild(target, sourceIndex);
        string destination = cards.ContainsKey(target) ? "Exchange at" : "Move into empty slot at";
        feedback.Show(origin!, $"{destination} {Place(origin!)}. Release / Enter places; Escape cancels.");
    }

    private Control? SlotAt(Vector2 pointer)
    {
        if (!viewport.GetGlobalRect().HasPoint(pointer)) return null;
        Rect2[] bounds = [.. grid.GetChildren().OfType<Control>().Select(slot => slot.GetGlobalRect())];
        hoveredSlot = TargetSnapGeometry.Select(bounds, pointer, hoveredSlot);
        return hoveredSlot is { } index ? original[index] : null;
    }

    private void Complete(Vector2 pointer)
    {
        if (!moving)
        {
            CompleteClick(pointer);
            return;
        }
        if (SlotAt(pointer) is null) { Cancel(); return; }
        SourcePresentationOrder.Store(seat, grid.GetChildren().OfType<Control>()
            .Select(slot => cards.TryGetValue(slot, out CardControl? card) ? card.TargetId : null));
        result.PresentGestureFeedback("");
        Finish();
    }

    private void CompleteClick(Vector2 pointer)
    {
        CardControl? inspected = dragged;
        Button? action = clickedAction;
        Cancel();
        if (InteractionControl.IsUsable(action) && action!.GetGlobalRect().HasPoint(pointer))
            action.EmitSignal(BaseButton.SignalName.Pressed);
        else if (action is null && inspected is not null && CardPointerHitTest.Contains(inspected, pointer))
            result.InspectSource(inspected);
    }

    private void Cancel()
    {
        if (dragged is null) return;
        slotMotion.Capture(dragged);
        RestoreOrder();
        result.PresentGestureFeedback("");
        Finish(cancelled: true);
    }

    private void RestoreOrder()
    {
        for (int index = 0; index < original.Length; index++) grid.MoveChild(original[index], index);
    }

    private void Finish(bool cancelled = false)
    {
        if (InteractionControl.IsUsable(clickedAction)) clickedAction!.SetPressedNoSignal(false);
        clickedAction = null;
        pointerFeedback.Finish(cancelled && moving ? "Arrangement cancelled · returned" : "");
        feedback.Hide();
        result.SetSourceArrangement(false);
        lift.Restore();
        if (InteractionControl.IsUsable(dragged))
        {
            CardControl settling = dragged!;
            Vector2 released = settling.Position;
            dragged!.Position = Vector2.Zero;
            dragged.ZIndex = 0;
            result.UpdateRestingPose(dragged);
            if (moving && !keyboard && result.MotionEnabled)
            {
                settling.Position = released;
                returns.Settle(settling, Vector2.Zero, settling.Rotation,
                    () => result.UpdateRestingPose(settling));
            }
        }
        dragged = null;
        foreach (Control vacancy in vacancies) vacancy.QueueRedraw();
        origin = null;
        moving = false;
        original = [];
        hoveredSlot = null;
        swapped = null;
        if (keyboard && InteractionControl.IsUsable(handle)) CardFocusPreview.Restore(handle!);
        handle = null;
    }

    private string Identity(Control slot) => cards[slot].GetMeta("source_identity").AsString();
    private string Place(Control slot) => $"row {slot.GetIndex() / grid.Columns + 1}, column {slot.GetIndex() % grid.Columns + 1}";

    public override void _Ready() => GetWindow().FocusExited += Cancel;
    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut) Cancel();
    }
    public override void _ExitTree()
    {
        GetWindow().FocusExited -= Cancel;
        if (dragged is not null) Cancel();
        lift.Restore();
    }
}
