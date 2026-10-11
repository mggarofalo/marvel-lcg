using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns one accepted response's timed attention cues, independently of the decision draft.</summary>
internal sealed partial class EventActivityPresenter : Node
{
    private readonly EventActivitySequence sequence = new();
    private Main? main;
    private Control? host;
    private EventActivityPulse? pulse;
    private double elapsed;

    internal void Configure(Main owner)
    {
        main = owner;
        pulse = new EventActivityPulse { Name = "EventActivityPulse", MouseFilter = Control.MouseFilterEnum.Ignore,
            TopLevel = true, ZAsRelative = false, ZIndex = 2000 };
        AddChild(pulse);
    }

    internal void Present(IReadOnlyList<EventPresentation> entries)
    {
        sequence.Start(entries, main!.eventMotion.ButtonPressed);
        elapsed = 0;
        host = null;
        RefreshHost();
        SetProcess(entries.Count > 0);
        RefreshPulse();
    }

    public override void _Process(double delta)
    {
        if (!InteractionControl.IsUsable(main)) return;
        elapsed += delta;
        if (!main!.eventMotion.ButtonPressed && sequence.Active) Complete();
        bool changed = sequence.Advance(delta);
        if (RefreshHost() || changed) UpdateText();
        RefreshPulse();
    }

    internal void Complete()
    {
        sequence.Complete();
        UpdateText();
        RefreshPulse();
    }

    internal void Clear()
    {
        sequence.Clear();
        UpdateText();
        RefreshPulse();
        SetProcess(false);
    }

    private bool RefreshHost()
    {
        if (InteractionControl.IsUsable(host)) return false;
        host = main!.boardAreas.FindChild("EventActivity", true, false) as Control;
        UpdateText();
        return host is not null;
    }

    private void UpdateText()
    {
        if (InteractionControl.IsUsable(host)) EventActivitySurface.Present(host!, sequence);
    }

    private void RefreshPulse()
    {
        if (!InteractionControl.IsUsable(pulse)) return;
        List<Rect2> visible = [];
        if (sequence.Active && sequence.Current is { } entry && main!.boardRender is { } board)
            foreach (int id in entry.Anchors.Distinct())
                if (board.ControlFor(id) is { } card && VisibleBounds(card) is { } bounds)
                    visible.Add(bounds);
        pulse!.Present(visible, sequence.Index, elapsed);
    }

    private static Rect2? VisibleBounds(Control card)
    {
        if (!InteractionControl.IsUsable(card) || !card.IsVisibleInTree()) return null;
        Rect2 bounds = SpatialCardFootprint.Face(card).Intersection(card.GetViewportRect());
        for (Node? parent = card.GetParent(); parent is not null; parent = parent.GetParent())
            if (parent is Control clip && (clip.ClipContents || clip is ScrollContainer))
                bounds = bounds.Intersection(clip.GetGlobalRect());
        return bounds.HasArea() ? bounds : null;
    }
}
