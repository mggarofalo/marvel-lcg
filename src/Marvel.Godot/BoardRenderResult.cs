using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    private readonly Dictionary<int, Control> seats = [];
    private readonly Dictionary<Control, Action> areaExpanders = [];

    /// <summary>Raised with the card under the pointer, or null when it leaves.</summary>
    public event Action<BoardCardPresentation, Control>? CardActivated;

    /// <summary>Raised when the player explicitly chooses a workspace to view.</summary>
    public event Action<int, Control>? ViewedSeatRequested;

    /// <summary>Raised for a source-local action supplied by the current prompt.</summary>
    public event Action<int>? AffordanceRequested;

    /// <summary>Raised for an ordinary source-local target supplied by the current prompt.</summary>
    public event Action<int>? TargetRequested;

    /// <summary>Raised after presentation-only paging changes.</summary>
    public event Action<string>? RefreshRequested;

    /// <summary>The seat chosen for this render pass after applying safe defaults.</summary>
    public int? ViewedSeat { get; internal set; }

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
        control.GuiInput += input =>
        {
            bool pointer = input is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            };
            bool keyboard = input is InputEventKey { Echo: false }
                && input.IsActionPressed("ui_accept");
            if (pointer || keyboard)
            {
                CardActivated?.Invoke(card, control);
                control.AcceptEvent();
            }
        };
    }

    internal void RequestViewedSeat(int seat, Control source) =>
        ViewedSeatRequested?.Invoke(seat, source);

    internal void RegisterSeat(int seat, Control control) => seats[seat] = control;

    internal void RequestAffordance(int id) => AffordanceRequested?.Invoke(id);

    internal void RequestTarget(int id) => TargetRequested?.Invoke(id);

    internal void RequestRefresh(string focusName) => RefreshRequested?.Invoke(focusName);

    /// <summary>Returns the visible control for an engine-provided card id.</summary>
    public Control? ControlFor(int id) =>
        controls.TryGetValue(id, out List<CardControl>? matches)
            ? matches.LastOrDefault()
            : null;

    /// <summary>Returns the stable switcher control for one visible seat.</summary>
    public Control? SeatControlFor(int seat) => seats.GetValueOrDefault(seat);

    /// <summary>Highlights every visible control matching server-provided ids.</summary>
    public void Highlight(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> highlighted = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetHighlighted(highlighted.Contains(key));
                if (highlighted.Contains(key))
                {
                    EnsureVisible(match);
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
            foreach (CardControl match in matches)
            {
                match.SetPresented(presented.Contains(key));
                if (presented.Contains(key))
                {
                    EnsureVisible(match);
                }
            }
        }
    }

    private void EnsureVisible(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor is Control areaBody)
            {
                ExpandArea(areaBody);
            }
            if (ancestor is ScrollContainer scroll)
            {
                RevealInScroll(scroll, control);
            }

            ancestor = ancestor.GetParent();
        }
    }

    private void ExpandArea(Control areaBody)
    {
        if (areaExpanders.TryGetValue(areaBody, out Action? expand))
        {
            expand();
        }
    }

    private static void RevealInScroll(ScrollContainer scroll, Control control)
    {
        scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible,
            AreaContaining(scroll, control) ?? control);
    }

    private static Control? AreaContaining(ScrollContainer scroll, Control control)
    {
        Node? candidate = control;
        Control? area = null;
        while (candidate is not null && candidate != scroll)
        {
            if (candidate is PanelContainer panel
                && panel.Name.ToString().StartsWith("Area", StringComparison.Ordinal))
            {
                area = panel;
            }

            candidate = candidate.GetParent();
        }

        return area;
    }
}
