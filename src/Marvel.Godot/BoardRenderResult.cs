using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    internal readonly BoardInspectorSequences Inspector = new();
    private readonly Dictionary<Control, Action> areaExpanders = [];
    private readonly Dictionary<int, Button> mulliganToggles = [];
    private readonly Dictionary<int, CardControl> mulliganCards = [];
    private readonly HashSet<int> legalMulliganTargets = [];
    private readonly List<BoardDropTarget> dropTargets = [];
    private readonly BoardCardInteractionControls interactionControls;
    private readonly BoardControlReveal reveal;
    private Control? mulliganDiscard;
    private (Control Source, CardPointerCapture Gesture)? pointerCapture;
    private Func<CardPointerGesture, bool>? directActivation;
    private Func<CardPointerGesture, bool>? directDrag;
    public BoardRenderResult()
    {
        reveal = new BoardControlReveal(this);
        interactionControls = new BoardCardInteractionControls(IsCurrentRender);
    }

    /// <summary>Identifies whether this render remains the board currently shown by its owner.</summary>
    internal Func<bool>? IsCurrent { get; set; }

    /// <summary>Raised with the card under the pointer, or null when it leaves.</summary>
    public event Action<BoardCardPresentation, Control>? CardActivated;

    internal void Register(int id, CardControl control)
    {
        if (!controls.TryGetValue(id, out List<CardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    internal void RegisterArea(Control body, Action expand) =>
        areaExpanders.Add(body, expand);

    internal void RegisterDropTarget(int seat, Control control) =>
        dropTargets.Add(new BoardDropTarget(seat, control));

    internal void TrackCard(Control control, BoardCardPresentation card, bool isHandCard = false)
    {
        if (control is CardControl rendered) interactionControls.Track(rendered, card, isHandCard);
        control.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse)
            {
                if (!InteractiveDescendantOwnsPointer(control))
                {
                    BeginPointerCapture(control, card, isHandCard, mouse.GlobalPosition);
                }
            }
            else if (input is InputEventKey { Echo: false } && input.IsActionPressed("ui_accept")) Activate(control, card, isHandCard, Vector2.Zero);
        };
    }

    /// <summary>Routes a captured card gesture from the root input path.</summary>
    internal bool RoutePointer(InputEvent input)
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

        if (!IsCurrentRender() || !InteractionControl.IsUsable(captured.Source))
        {
            pointerCapture = null;
            return false;
        }

        if (input is InputEventMouseMotion)
        {
            return true;
        }

        if (input is not InputEventMouseButton
            { ButtonIndex: MouseButton.Left, Pressed: false } release)
        {
            return false;
        }

        // Clear first: an adapter can synchronously rebuild the board, and a
        // release must name at most one prompt-bound operation.
        pointerCapture = null;
        if (!captured.Gesture.TryReleaseAt(release.GlobalPosition, out bool isDrag))
        {
            return true;
        }

        if (isDrag)
        {
            TryDrag(captured.Source, captured.Gesture.Card, captured.Gesture.IsHandCard,
                release.GlobalPosition, captured.Gesture.Start);
        }
        else
        {
            Activate(captured.Source, captured.Gesture.Card, captured.Gesture.IsHandCard,
                release.GlobalPosition);
        }
        return true;
    }

    /// <summary>Replaces card-attached controls and cues from the current authorized draft.</summary>
    internal void PresentInteraction(DecisionComposer? composer, PromptPresentation? prompt)
    {
        interactionControls.Present(controls, composer, prompt);
        InteractionRelationshipsChanged?.Invoke(
            BoardInteractionRelationshipProjection.From(composer, prompt));
    }

    internal event Action<IReadOnlyList<TableRelationshipDescriptor>>? InteractionRelationshipsChanged;
    private void BeginPointerCapture(
        Control control, BoardCardPresentation card, bool isHandCard, Vector2 start)
    {
        if (IsCurrentRender() && InteractionControl.IsUsable(control))
        {
            pointerCapture = (control, new CardPointerCapture(card, isHandCard, start));
        }
    }

    private static bool InteractiveDescendantOwnsPointer(Control card) =>
        card.GetViewport().GuiGetHoveredControl() is BaseButton hovered
        && hovered != card && card.IsAncestorOf(hovered);

    private bool TryDrag(Control control, BoardCardPresentation card, bool isHandCard, Vector2 finish, Vector2 start)
    {
        if (IsCurrentRender() && CardPointerGestureRouter.IsDrag(start, finish)
            && directDrag?.Invoke(new CardPointerGesture(card, control, isHandCard, finish)) == true)
        {
            return true;
        }

        if (!IsCandidateDrag(card, start, finish, out int id)
            || !mulliganCards.TryGetValue(id, out CardControl? dragged)
            || !DragControlsAreUsable(dragged, finish)) return false;
        MulliganTargetRequested?.Invoke(id);
        return true;
    }

    private bool IsCandidateDrag(
        BoardCardPresentation card, Vector2 start, Vector2 finish, out int id)
    {
        id = card.TargetId ?? -1;
        return IsCurrentRender() && CardPointerGestureRouter.IsDrag(start, finish)
            && card.TargetId is not null && legalMulliganTargets.Contains(id);
    }

    private bool DragControlsAreUsable(CardControl dragged, Vector2 finish) =>
        InteractionControl.IsUsable(dragged) && InteractionControl.IsUsable(mulliganDiscard)
        && mulliganDiscard!.GetGlobalRect().HasPoint(finish);

    private void Activate(Control control, BoardCardPresentation card, bool isHandCard, Vector2 position)
    {
        if (!IsCurrentRender() || !InteractionControl.IsUsable(control)) return;
        if (directActivation?.Invoke(new CardPointerGesture(card, control, isHandCard, position)) == true)
        {
            control.AcceptEvent();
            return;
        }
        CardActivated?.Invoke(card, control);
        control.AcceptEvent();
    }

    private bool IsCurrentRender() => IsCurrent?.Invoke() == true;

    /// <summary>Raised when a tabletop mulligan checkbox or discard drag names a visible hand card.</summary>
    internal event Action<int>? MulliganTargetRequested;

    internal void BindDirectInteractions(
        Func<CardPointerGesture, bool> activate,
        Func<CardPointerGesture, bool> drag)
    {
        directActivation = activate ?? throw new ArgumentNullException(nameof(activate));
        directDrag = drag ?? throw new ArgumentNullException(nameof(drag));
    }

    internal void BindExplicitInteraction(Func<CardPointerGesture, bool> activate) =>
        interactionControls.Bind(activate);

    internal bool IsDroppedOnLivePlayerLane(int seat, Vector2 position) =>
        dropTargets.Any(target => target.Seat == seat
            && InteractionControl.IsUsable(target.Control)
            && target.Control.GetGlobalRect().HasPoint(position));

    internal void RegisterMulliganToggle(int id, Button toggle) => mulliganToggles[id] = toggle;

    internal void RegisterMulliganCard(int id, CardControl card) => mulliganCards[id] = card;

    internal void RegisterMulliganDiscard(Control discard) => mulliganDiscard = discard;

    internal void BindMulliganTargets(
        IReadOnlyCollection<int> legal, IReadOnlyCollection<int> selected, Action<int> choose)
    {
        legalMulliganTargets.Clear();
        legalMulliganTargets.UnionWith(legal);
        MulliganTargetRequested = choose;
        foreach ((int id, Button toggle) in mulliganToggles)
        {
            bool available = legal.Contains(id);
            toggle.Visible = available;
            toggle.Disabled = !available;
            SetMulliganToggle(toggle, selected.Contains(id));
        }
    }

    internal void RequestMulliganTarget(int id)
    {
        if (legalMulliganTargets.Contains(id))
        {
            MulliganTargetRequested?.Invoke(id);
        }
    }

    internal void SetMulliganTargets(IReadOnlyCollection<int> selected)
    {
        foreach ((int id, Button toggle) in mulliganToggles)
        {
            SetMulliganToggle(toggle, selected.Contains(id));
        }
    }

    private static void SetMulliganToggle(Button toggle, bool selected)
    {
        toggle.SetPressedNoSignal(selected);
        toggle.Text = selected ? "✓ DISCARD" : "□ DISCARD";
        toggle.ThemeTypeVariation = selected
            ? GodotThemeVariations.SelectedTargetButton
            : GodotThemeVariations.LegalTargetButton;
    }

    /// <summary>Returns the visible control for an engine-provided card id.</summary>
    public Control? ControlFor(int id) =>
        controls.TryGetValue(id, out List<CardControl>? matches)
            ? matches.LastOrDefault(InteractionControl.IsUsable)
            : null;

    internal IReadOnlyList<CardControl> VisibleCardControls() =>
        [.. controls.Values.SelectMany(matches => matches).Where(InteractionControl.IsUsable)];

    /// <summary>Highlights every visible control matching server-provided ids.</summary>
    public void Highlight(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> highlighted = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                match.SetHighlighted(highlighted.Contains(key));
                if (highlighted.Contains(key))
                {
                    reveal.Ensure(key);
                }
            }
        }
    }

    /// <summary>Marks cards for a transient event cue without disturbing prompt focus.</summary>
    public void Present(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> presented = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                match.SetPresented(presented.Contains(key));
                if (presented.Contains(key))
                {
                    reveal.Ensure(key);
                }
            }
        }
    }

    internal void ExpandArea(Control areaBody)
    {
        if (areaExpanders.TryGetValue(areaBody, out Action? expand))
        {
            expand();
        }
    }

    internal bool TryCurrentControl(int key, out Control? control)
    {
        control = IsCurrent?.Invoke() == true ? ControlFor(key) : null;
        return control is not null;
    }

}
