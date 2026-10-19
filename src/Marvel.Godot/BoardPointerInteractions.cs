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
    private readonly CardReturnMotion returns = new();
    private readonly DragPointerFeedback feedback = new();
    private readonly BoardPointerDropSurfaces drops = new();
    private readonly CardPointerRestingPoses resting = new();
    private bool localDestination;

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
        resting.Store(control);
        control.TreeExiting += () => returns.Stop(control);
        if (isHandCard && control is CardControl handCard) hand.Track(handCard);
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
        resting.Store(control);
    }

    internal void TrackPreviewOnly(Control control, BoardCardPresentation card)
    {
        control.MouseEntered += () => previewEntered(card, control);
        control.MouseExited += () => previewExited(control);
        CardFocusPreview.Bind(control, () => previewEntered(card, control), () => previewExited(control));
    }

    internal bool Route(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right, Pressed: true })
        {
            CancelCaptured();
            return false;
        }
        if (pointerCapture is not { } captured)
        {
            return false;
        }
        if (!isCurrent() || !InteractionControl.IsUsable(captured.Source))
        {
            CancelCaptured();
            return false;
        }
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            CancelCaptured();
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
        return ReleaseCaptured(captured, release.GlobalPosition);
    }

    private bool ReleaseCaptured(
        (Control Source, CardPointerCapture Gesture) captured, Vector2 position)
    {
        if (!captured.Gesture.TryReleaseAt(position, out bool isDrag))
        {
            return true;
        }
        if (isDrag)
        {
            lift.Restore();
            // Interaction policy: returning to the grab point cancels the local
            // gesture, preserving the draft and the pre-drag presentation order.
            bool returnedToOrigin = !CardPointerGestureRouter.IsDrag(
                captured.Gesture.Start, position);
            if (returnedToOrigin) hand.CancelPreview();
            bool accepted = !returnedToOrigin && CardDropCompletion.TryDrop(
                new CardPointerGesture(captured.Gesture.Card, captured.Source,
                    captured.Gesture.IsHandCard, position), isCurrent(), mulligan, drops, hand, directDrag);
            ReturnCaptured(captured.Source, invalid: !accepted && !returnedToOrigin,
                cancelled: returnedToOrigin);
        }
        else
        {
            resting.Restore(captured.Source);
            captured.Source.Modulate = Colors.White;
            Activate(captured.Source, captured.Gesture.Card, captured.Gesture.IsHandCard,
                position);
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
            if (input is InputEventKey { Keycode: Key.Space }
                && card.TargetId is { } id && mulligan.Contains(id))
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
            returns.Stop(control);
            hand.ReleasePointer(control);
            pointerCapture = (control, new CardPointerCapture(card, isHandCard, start));
            pointerLifted = false;
        }
    }

    private static bool InteractiveDescendantOwnsPointer(Control card) =>
        card.GetViewport().GuiGetHoveredControl() is BaseButton hovered
        && hovered != card && card.IsAncestorOf(hovered);

    private void Activate(
        Control control, BoardCardPresentation card, bool isHandCard, Vector2 position)
    {
        if (!isCurrent() || !InteractionControl.IsUsable(control)) return;
        if (directActivation?.Invoke(new CardPointerGesture(card, control, isHandCard, position)) == true)
        {
            control.AcceptEvent();
            return;
        }
        if (position == Vector2.Zero) activated(card, control);
        else previewEntered(card, control);
        control.AcceptEvent();
    }

    private void EnterCard(Control control, BoardCardPresentation card)
    {
        if (!isCurrent() || card.Concealed || pointerCapture is not null
            || InteractiveDescendantOwnsPointer(control)) return;
        if (resting.TryGetValue(control, out var pose))
        {
            BoardHoverPresentation.Raise(control, SpatialCardPose.RestingZ(control, pose.Z));
        }
        control.MouseDefaultCursorShape = card.TargetId is { } id && mulligan.Contains(id)
            || canDrag?.Invoke(new CardPointerGesture(card, control, control.HasMeta("spatial_hand_index"),
                control.GetGlobalMousePosition())) == true ? Control.CursorShape.Move : Control.CursorShape.PointingHand;
        previewEntered(card, control);
    }

    private void ExitCard(Control control)
    {
        if (isCurrent() && pointerCapture is null) resting.Restore(control);
        BoardHoverPresentation.RestoreControls(control);
        previewExited(control);
    }

    private void MoveCaptured(
        (Control Source, CardPointerCapture Gesture) captured, Vector2 pointer)
    {
        if (!captured.Gesture.ObserveAt(pointer)) return;
        if (!pointerLifted) BeginLift(captured, pointer);
        lift.Move(captured.Source, pointer);
        if (captured.Gesture.IsHandCard) hand.Preview(captured.Source, pointer);
        if (captured.Source is CardControl handCard) SpatialHandActionStrip.Refresh(handCard);
        localDestination = captured.Gesture.IsHandCard && hand.Contains(pointer)
            || captured.Gesture.Card.TargetId is { } target && mulligan.Contains(target) && drops.ContainsMulligan(pointer);
        feedback.Preview(pointer, localDestination);
        DragPreview?.Invoke(new CardPointerGesture(captured.Gesture.Card, captured.Source,
            captured.Gesture.IsHandCard, pointer));
    }

    private void BeginLift((Control Source, CardPointerCapture Gesture) captured, Vector2 pointer)
    {
        pointerLifted = true;
        lift.Begin(captured.Source, captured.Gesture.Start);
        feedback.Begin(captured.Source, CancelCaptured);
        bool playAvailable = canDrag?.Invoke(new CardPointerGesture(
            captured.Gesture.Card, captured.Source, captured.Gesture.IsHandCard, pointer)) == true;
        drops.SetActive(playAvailable);
        drops.SetMulliganActive(captured.Gesture.Card.TargetId is { } id
            && mulligan.Contains(id));
    }

    internal void PresentDestination(CardPointerGesture gesture, bool accepted) => feedback.Preview(gesture.Position, accepted || localDestination);

    private void CancelCaptured()
    {
        if (pointerCapture is not { } captured) return;
        pointerCapture = null;
        hand.CancelPreview();
        ReturnCaptured(captured.Source, invalid: false, cancelled: true);
    }

    private void ReturnCaptured(Control control, bool invalid, bool cancelled = false)
    {
        hand.ReleasePointer(control);
        lift.Restore();
        drops.SetActive(false);
        drops.SetMulliganActive(false);
        pointerLifted = false;
        feedback.Finish(invalid ? "No offered action here · returned" : cancelled ? "Drag cancelled · returned" : "");
        DragFinished?.Invoke(invalid);
        if (!resting.TryGetValue(control, out var pose) || !InteractionControl.IsUsable(control)) return;
        if (MotionEnabled?.Invoke() == false)
        {
            resting.Restore(control);
            control.Modulate = Colors.White;
            return;
        }
        if (invalid) control.Modulate = ClientTheme.ToGodot(VisualSystem.Palette.Danger);
        returns.Settle(control, pose.Position, pose.Rotation, () => resting.Restore(control));
    }

}
