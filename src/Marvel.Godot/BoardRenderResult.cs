using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    private readonly Dictionary<Control, Action> areaExpanders = [];
    private readonly BoardControlReveal reveal;

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
        control.GuiInput += input =>
        {
            bool pointer = input is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            };
            bool keyboard = input is InputEventKey { Echo: false }
                && input.IsActionPressed("ui_accept");
            if ((pointer || keyboard) && IsCurrent?.Invoke() == true
                && InteractionControl.IsUsable(control))
            {
                CardActivated?.Invoke(card, control);
                control.AcceptEvent();
            }
        };
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
