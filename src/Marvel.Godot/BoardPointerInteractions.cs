using Godot;
using Marvel.View;

namespace Marvel.Godot;

internal sealed class BoardPointerInteractions
{
    private readonly Func<bool> isCurrent;
    private readonly Action<BoardCardPresentation, Control> activated;
    private readonly Action<BoardCardPresentation, Control> previewEntered;
    private readonly Action<Control> previewExited;
    private readonly Dictionary<int, CardControl> mulliganCards = [];
    private readonly HashSet<int> legalMulliganTargets = [];
    private readonly BoardPointerDropSurfaces drops = new();
    private readonly Dictionary<Control, (Vector2 Position, float Rotation, int Z)> resting = [];

    private (Control Source, CardPointerCapture Gesture)? pointerCapture;
    private Func<CardPointerGesture, bool>? directActivation;
    private Func<CardPointerGesture, bool>? directDrag;
    private Func<CardPointerGesture, bool>? canDrag;
    private Action<int>? mulliganTargetRequested;
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
    }
    internal void Track(Control control, BoardCardPresentation card, bool isHandCard)
    {
        resting[control] = (control.Position, control.Rotation, control.ZIndex);
        SpatialCardPose.Store(control);
        control.MouseEntered += () => EnterCard(control, card);
        control.MouseExited += () => ExitCard(control);
        control.GuiInput += input => HandleCardInput(control, card, isHandCard, input);
    }

    internal void UpdateRestingPose(Control control)
    {
        resting[control] = (control.Position, control.Rotation, control.ZIndex);
        SpatialCardPose.Store(control);
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
            pointerCapture = null;
            return false;
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

    internal void RegisterMulliganCard(int id, CardControl card) => mulliganCards[id] = card;

    internal void RegisterMulliganDiscard(Control discard) => drops.RegisterMulligan(discard);

    internal void BindMulliganTargets(IReadOnlyCollection<int> legal, Action<int> choose) {
        legalMulliganTargets.Clear();
        legalMulliganTargets.UnionWith(legal);
        mulliganTargetRequested = choose;
    }

    internal void RequestMulliganTarget(int id) {
        if (legalMulliganTargets.Contains(id))
        {
            mulliganTargetRequested?.Invoke(id);
        }
    }

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
        if (isCurrent() && dragged
            && directDrag?.Invoke(new CardPointerGesture(card, control, isHandCard, finish)) == true)
        {
            return true;
        }
        int id = card.TargetId ?? -1;
        if (!CanDropMulligan(card, id, finish, dragged))
        {
            return false;
        }
        mulliganTargetRequested?.Invoke(id);
        return true;
    }

    private bool CanDropMulligan(
        BoardCardPresentation card, int id, Vector2 finish, bool dragged) =>
        isCurrent() && dragged && card.TargetId is not null
        && legalMulliganTargets.Contains(id)
        && mulliganCards.TryGetValue(id, out CardControl? source)
        && InteractionControl.IsUsable(source)
        && drops.ContainsMulligan(finish);

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
            captured.Source.ZIndex = 240;
            captured.Source.Rotation = 0;
            captured.Source.Modulate = Colors.White;
            bool playAvailable = canDrag?.Invoke(new CardPointerGesture(
                captured.Gesture.Card, captured.Source, captured.Gesture.IsHandCard, pointer)) == true;
            drops.SetActive(playAvailable);
            drops.SetMulliganActive(captured.Gesture.Card.TargetId is { } id
                && legalMulliganTargets.Contains(id));
        }
        captured.Source.GlobalPosition = pointer - captured.Source.Size / 2;
        if (captured.Source is CardControl hand) SpatialHandActionStrip.Refresh(hand);
        DragPreview?.Invoke(new CardPointerGesture(captured.Gesture.Card, captured.Source,
            captured.Gesture.IsHandCard, pointer));
    }

    private void ReturnCaptured(Control control, bool invalid)
    {
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
