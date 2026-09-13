using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    private readonly Dictionary<Control, Action> areaExpanders = [];
    private readonly Dictionary<int, Button> mulliganToggles = [];
    private readonly Dictionary<int, CardControl> mulliganCards = [];
    private readonly BoardControlReveal reveal;
    private Control? mulliganDiscard;

    public BoardRenderResult()
    {
        reveal = new BoardControlReveal(this);
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

    internal void TrackCard(Control control, BoardCardPresentation card)
    {
        Vector2? pressedAt = null;
        control.GuiInput += input =>
        {
            if (input is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
            } mouse)
            {
                if (mouse.Pressed)
                {
                    pressedAt = mouse.GlobalPosition;
                    return;
                }

                if (pressedAt is not { } start)
                {
                    return;
                }

                pressedAt = null;
                if (mulliganCards.TryGetValue(card.TargetId ?? int.MinValue, out CardControl? dragged)
                    && InteractionControl.IsUsable(mulliganDiscard)
                    && mulliganDiscard!.GetGlobalRect().HasPoint(mouse.GlobalPosition)
                    && start.DistanceTo(mouse.GlobalPosition) >= 10)
                {
                    MulliganTargetRequested?.Invoke(dragged.TargetId!.Value);
                    control.AcceptEvent();
                    return;
                }

                if (start.DistanceTo(mouse.GlobalPosition) < 10
                    && IsCurrent?.Invoke() == true && InteractionControl.IsUsable(control))
                {
                    CardActivated?.Invoke(card, control);
                    control.AcceptEvent();
                }
                return;
            }

            bool keyboard = input is InputEventKey { Echo: false }
                && input.IsActionPressed("ui_accept");
            if (keyboard && IsCurrent?.Invoke() == true
                && InteractionControl.IsUsable(control))
            {
                CardActivated?.Invoke(card, control);
                control.AcceptEvent();
            }
        };
    }

    /// <summary>Raised when a tabletop mulligan checkbox or discard drag names a visible hand card.</summary>
    internal event Action<int>? MulliganTargetRequested;

    internal void RegisterMulliganToggle(int id, Button toggle) => mulliganToggles[id] = toggle;

    internal void RegisterMulliganCard(int id, CardControl card) => mulliganCards[id] = card;

    internal void RegisterMulliganDiscard(Control discard) => mulliganDiscard = discard;

    internal void BindMulliganTargets(
        IReadOnlyCollection<int> legal, IReadOnlyCollection<int> selected, Action<int> choose)
    {
        MulliganTargetRequested = choose;
        foreach ((int id, Button toggle) in mulliganToggles)
        {
            bool available = legal.Contains(id);
            toggle.Visible = available;
            toggle.Disabled = !available;
            SetMulliganToggle(toggle, selected.Contains(id));
        }
    }

    internal void RequestMulliganTarget(int id) => MulliganTargetRequested?.Invoke(id);

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
