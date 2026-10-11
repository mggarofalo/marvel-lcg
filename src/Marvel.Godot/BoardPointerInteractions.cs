using Godot;
using Marvel.View;

namespace Marvel.Godot;

internal sealed class BoardPointerInteractions
{
    private readonly Func<bool> isCurrent;
    private readonly Action<BoardCardPresentation, Control> activated;
    private readonly Action<BoardCardPresentation, Control> previewEntered;
    private readonly Action<Control> previewExited;
    private readonly PointerMulliganDraft mulligan = new();
    private readonly HandGestureLayout hand;
    private readonly CardPointerLift lift = new();
    private readonly BoardPointerDropSurfaces drops = new();
    private readonly Dictionary<Control, (Vector2 Position, float Rotation, int Z)> resting = [];

    private (Control Source, CardPointerCapture Gesture)? pointerCapture;
    private Func<CardPointerGesture, bool>? directActivation;
    private Func<CardPointerGesture, bool>? directDrag;
    private Func<CardPointerGesture, bool>? canDrag;
    private bool pointerLifted;
    internal Func<bool>? MotionEnabled { get; set; }
    internal Action<CardPointerGesture>? DragPreview { get; set; }
    internal Action<bool>? DragFinished { get; set; }

    internal BoardPointerInteractions(
        Func<bool> isCurrent,
        Action<BoardCardPresentation, Control> activated,
        Action<BoardCardPresentation, Control> previewEntered,
        Action<Control> previewExited)
    {
        this.isCurrent = isCurrent;
        this.activated = activated;
        this.previewEntered = previewEntered;
        this.previewExited = previewExited;
        hand = new HandGestureLayout(StoreRestingPose) { MotionEnabled = () => MotionEnabled?.Invoke() != false };
    }
    internal void Track(Control control, BoardCardPresentation card, bool isHandCard)
    {
        resting[control] = (control.Position, control.Rotation, control.ZIndex);
        if (isHandCard && control is CardControl handCard) hand.Track(handCard);
        SpatialCardPose.Store(control);
        control.MouseEntered += () => EnterCard(control, card);
        control.MouseExited += () => ExitCard(control);
        CardFocusPreview.Bind(control, () => EnterCard(control, card), () => ExitCard(control));
        control.GuiInput += input => HandleCardInput(control, card, isHandCard, input);
    }

    internal void UpdateRestingPose(Control control)
    {
        StoreRestingPose(control);
        hand.Capture(control);
    }

    private void StoreRestingPose(Control control)
    {
        resting[control] = (control.Position, control.Rotation, control.ZIndex);
        SpatialCardPose.Store(control);
    }

    internal void TrackPreviewOnly(Control control, BoardCardPresentation card)
    {
        control.MouseEntered += () => previewEntered(card, control);
        control.MouseExited += () => previewExited(control);
        CardFocusPreview.Bind(control, () => previewEntered(card, control), () => previewExited(control));
    }

    internal bool Route(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            pointerCapture = null;
            return false;
        }
        if (pointerCapture is not { } captured)
        {
            return false;
        }
        if (!isCurrent() || !InteractionControl.IsUsable(captured.Source))
        {
            lift.Restore();
            pointerCapture = null;
            return false;
        }
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            pointerCapture = null;
            hand.CancelPreview();
            ReturnCaptured(captured.Source, invalid: false);
            return true;
        }
        if (input is InputEventMouseMotion motion)
        {
            MoveCaptured(captured, motion.GlobalPosition);
            return true;
        }
        if (input is not InputEventMouseButton
            { ButtonIndex: MouseButton.Left, Pressed: false } release)
        {
            return false;
        }

        pointerCapture = null;
        if (!captured.Gesture.TryReleaseAt(release.GlobalPosition, out bool isDrag))
        {
            return true;
        }
        if (isDrag)
        {
            lift.Restore();
            bool accepted = TryDrag(captured.Source, captured.Gesture.Card,
                captured.Gesture.IsHandCard, release.GlobalPosition, captured.Gesture.Start);
            ReturnCaptured(captured.Source, invalid: !accepted);
        }
        else
        {
            Activate(captured.Source, captured.Gesture.Card, captured.Gesture.IsHandCard,
                release.GlobalPosition);
        }
        return true;
    }

    internal void BindDirect(
        Func<CardPointerGesture, bool> dragAvailable,
        Func<CardPointerGesture, bool> activate,
        Func<CardPointerGesture, bool> drag)
    {
        canDrag = dragAvailable ?? throw new ArgumentNullException(nameof(dragAvailable));
        directActivation = activate ?? throw new ArgumentNullException(nameof(activate));
        directDrag = drag ?? throw new ArgumentNullException(nameof(drag));
    }

    internal void RegisterDropTarget(int seat, Control control) => drops.Register(seat, control);

    internal bool IsDroppedOnLivePlayerLane(int seat, Vector2 position) => drops.Contains(seat, position);

    internal void RegisterMulliganCard(int id, CardControl card) => hand.Track(card);

    internal void RegisterHandArea(Control control, int seat,
        Func<int, int, float, SpatialCardPlacement> placement)
    {
        hand.RegisterArea(control, seat);
        hand.Placement = placement;
    }
    internal void RegisterMulliganStage(Control control) { hand.RegisterStage(control); drops.RegisterStage(control); }

    internal void RegisterMulliganDiscard(Control discard) => drops.RegisterMulligan(discard);

    internal void BindMulliganTargets(IReadOnlyCollection<int> legal, IReadOnlyCollection<int> selected, Action<int> choose) {
        mulligan.Bind(legal, selected, choose);
        hand.Stage(selected);
    }

    internal void RequestMulliganTarget(int id) => mulligan.Toggle(id);
    internal void SetMulliganTargets(IReadOnlyCollection<int> selected) { mulligan.SetSelected(selected); hand.Stage(selected); }

    private void HandleCardInput(
        Control control, BoardCardPresentation card, bool isHandCard, InputEvent input)
    {
        if (input is InputEventMouseButton
            { ButtonIndex: MouseButton.Left, Pressed: true } mouse)
        {
            if (!InteractiveDescendantOwnsPointer(control))
            {
                BeginPointerCapture(control, card, isHandCard, mouse.GlobalPosition);
            }
        }
        else if (input is InputEventKey { Echo: false } && input.IsActionPressed("ui_accept"))
        {
            if (card.TargetId is { } id && mulligan.Contains(id))
            {
                mulligan.Toggle(id);
                control.AcceptEvent();
                return;
            }
            Activate(control, card, isHandCard, Vector2.Zero);
        }
    }

    private void BeginPointerCapture(
        Control control, BoardCardPresentation card, bool isHandCard, Vector2 start)
    {
        if (isCurrent() && InteractionControl.IsUsable(control))
        {
            pointerCapture = (control, new CardPointerCapture(card, isHandCard, start));
            pointerLifted = false;
        }
    }

    private static bool InteractiveDescendantOwnsPointer(Control card) =>
        card.GetViewport().GuiGetHoveredControl() is BaseButton hovered
        && hovered != card && card.IsAncestorOf(hovered);

    private bool TryDrag(
        Control control, BoardCardPresentation card, bool isHandCard, Vector2 finish, Vector2 start)
    {
        bool dragged = CardPointerGestureRouter.IsDrag(start, finish);
        if (!isCurrent() || !dragged) return false;
        if (card.TargetId is { } id && mulligan.Drop(id, drops.ContainsMulligan(finish), hand.Contains(finish))) return true;
        if (isHandCard && hand.Drop(control, finish)) return true;
        hand.CancelPreview();
        return directDrag?.Invoke(new CardPointerGesture(card, control, isHandCard, finish)) == true;
    }

    private void Activate(
        Control control, BoardCardPresentation card, bool isHandCard, Vector2 position)
    {
        if (!isCurrent() || !InteractionControl.IsUsable(control)) return;
        if (directActivation?.Invoke(new CardPointerGesture(card, control, isHandCard, position)) == true)
        {
            control.AcceptEvent();
            return;
        }
        activated(card, control);
        control.AcceptEvent();
    }

    private void EnterCard(Control control, BoardCardPresentation card)
    {
        if (!isCurrent() || card.Concealed || pointerCapture is not null
            || InteractiveDescendantOwnsPointer(control)) return;
        if (resting.TryGetValue(control, out var pose))
        {
            BoardHoverPresentation.Raise(control, RestingZ(control, pose.Z));
        }
        previewEntered(card, control);
    }

    private void ExitCard(Control control)
    {
        if (pointerCapture is null) RestorePose(control);
        BoardHoverPresentation.RestoreControls(control);
        previewExited(control);
    }

    private void MoveCaptured(
        (Control Source, CardPointerCapture Gesture) captured, Vector2 pointer)
    {
        if (!CardPointerGestureRouter.IsDrag(captured.Gesture.Start, pointer)) return;
        if (!pointerLifted)
        {
            pointerLifted = true;
            lift.Begin(captured.Source);
            bool playAvailable = canDrag?.Invoke(new CardPointerGesture(
                captured.Gesture.Card, captured.Source, captured.Gesture.IsHandCard, pointer)) == true;
            drops.SetActive(playAvailable);
            drops.SetMulliganActive(captured.Gesture.Card.TargetId is { } id
                && mulligan.Contains(id));
        }
        CardPointerLift.Move(captured.Source, pointer);
        if (captured.Gesture.IsHandCard) hand.Preview(captured.Source, pointer);
        if (captured.Source is CardControl handCard) SpatialHandActionStrip.Refresh(handCard);
        DragPreview?.Invoke(new CardPointerGesture(captured.Gesture.Card, captured.Source,
            captured.Gesture.IsHandCard, pointer));
    }

    private void ReturnCaptured(Control control, bool invalid)
    {
        lift.Restore();
        drops.SetActive(false);
        drops.SetMulliganActive(false);
        pointerLifted = false;
        DragFinished?.Invoke(invalid);
        if (!resting.TryGetValue(control, out var pose) || !InteractionControl.IsUsable(control)) return;
        if (MotionEnabled?.Invoke() == false)
        {
            RestorePose(control);
            control.Modulate = Colors.White;
            return;
        }
        if (invalid) control.Modulate = ClientTheme.ToGodot(VisualSystem.Palette.Danger);
        Tween tween = control.CreateTween().SetParallel();
        tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(control, "position", pose.Position, 0.18);
        tween.TweenProperty(control, "rotation", pose.Rotation, 0.18);
        tween.TweenProperty(control, "modulate", Colors.White, 0.18);
        tween.Finished += () => RestorePose(control);
    }

    private void RestorePose(Control control) {
        if (!resting.TryGetValue(control, out var pose) || !InteractionControl.IsUsable(control)) return;
        control.Position = pose.Position;
        control.Rotation = pose.Rotation;
        if (control is CardControl hand) SpatialHandActionStrip.Refresh(hand);
        control.ZIndex = RestingZ(control, pose.Z);
    }

    private static int RestingZ(Control control, int fallback) =>
        SpatialCardPose.RestingZ(control, fallback);

}
