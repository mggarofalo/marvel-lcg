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
    private Button? handle;
    private SourceArrangementFeedback feedback = null!;

    internal void Configure(GridContainer container, ScrollContainer scroll, int player, BoardRenderResult board)
    {
        grid = container;
        viewport = scroll;
        seat = player;
        result = board;
        feedback = new SourceArrangementFeedback(scroll.GetParent<Control>());
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
        var mark = new Label { Name = "SourceReorderMark", Text = "⠿", MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        mark.AddThemeFontSizeOverride("font_size", 16);
        mark.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        overlay.AddChild(mark);
        void Fit()
        {
            grip.Position = header.Position;
            grip.Size = new Vector2(title.Position.X + title.Size.X + 7, title.Size.Y);
            mark.Position = header.Position + new Vector2(header.Size.X - 24, 0);
            mark.Size = new Vector2(24, title.Size.Y);
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
    }

    private void Begin(CardControl card, Control slot, Button grip, Vector2 point, bool usingKeyboard)
    {
        Cancel();
        dragged = card;
        origin = slot;
        handle = grip;
        start = point;
        keyboard = usingKeyboard;
        original = [.. grid.GetChildren().OfType<Control>()];
        result.SetSourceArrangement(true);
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
        if (!moving) { lift.Begin(dragged); moving = true; }
        CardPointerLift.Move(dragged, pointer);
        dragged.GlobalPosition -= new Vector2(dragged.Size.X / 2 + 16, 0);
        Control? target = SlotAt(pointer);
        if (target is null)
        {
            feedback.Show(origin!, "Outside the source grid · release to cancel. Your table order is unchanged.");
            return;
        }
        SwapWith(target);
    }

    private void SwapWith(Control target)
    {
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
        feedback.Show(origin!, $"Place in {Place(origin!)}. Release / Enter places; Escape cancels.");
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
        result.PresentGestureFeedback("");
        Finish();
    }

    private void Cancel()
    {
        if (dragged is null) return;
        RestoreOrder();
        result.PresentGestureFeedback("");
        Finish();
    }

    private void RestoreOrder()
    {
        for (int index = 0; index < original.Length; index++) grid.MoveChild(original[index], index);
    }

    private void Finish()
    {
        feedback.Hide();
        result.SetSourceArrangement(false);
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
