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
    private bool suppressObjects;

    internal void Configure(Main owner)
    {
        main = owner;
        pulse = new EventActivityPulse { Name = "EventActivityPulse", MouseFilter = Control.MouseFilterEnum.Ignore,
            TopLevel = true, ZAsRelative = false, ZIndex = 2000 };
        AddChild(pulse);
        SetProcess(false);
    }

    internal void Present(IReadOnlyList<EventPresentation> entries)
    {
        sequence.Start(entries, main!.eventMotion.ButtonPressed);
        suppressObjects = false;
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
        if (changed) elapsed = 0;
        if (RefreshHost() || changed) UpdateText();
        RefreshPulse();
    }

    internal void Complete()
    {
        sequence.Complete();
        UpdateText();
        RefreshPulse();
    }

    internal void SettleForDraft()
    {
        suppressObjects = true;
        Complete();
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
        var visible = VisibleSubjects(CurrentEntries());
        if (!sequence.Active) visible = SettledSubjects(visible);
        pulse!.Present(visible, elapsed, sequence.Active, host, StageEntries());
    }

    private IReadOnlyList<EventPresentation> CurrentEntries() =>
        sequence.Active && sequence.Current is { } current ? [current] : sequence.Entries;

    private IReadOnlyList<EventPresentation> StageEntries() =>
        sequence.Active && sequence.Current is { } current ? [current] : ActivityVisualMeaning.Settled(sequence.Entries);

    private List<(EventPresentation, ActivitySubject, Rect2)> VisibleSubjects(IReadOnlyList<EventPresentation> entries)
    {
        List<(EventPresentation, ActivitySubject, Rect2)> visible = [];
        if (suppressObjects || main!.boardRender is not { } board) return visible;
        foreach (EventPresentation entry in entries)
            foreach (ActivitySubject subject in entry.Activity?.Subjects ?? [])
                if (board.ControlFor(subject.Card) is { } card && VisibleBounds(card) is { } bounds)
                    visible.Add((entry, subject, bounds));
        return visible;
    }

    private static List<(EventPresentation, ActivitySubject, Rect2)> SettledSubjects(
        List<(EventPresentation, ActivitySubject, Rect2)> visible) =>
        visible.Where(reference => reference.Item1.Activity!.Quantities.Count > 0
                || reference.Item1.Motion is EventMotionKind.Status or EventMotionKind.Attack or EventMotionKind.Defeat)
            .GroupBy(reference => (reference.Item2.Card, reference.Item2.Role, reference.Item1.Motion, reference.Item1.Activity!.StatusChange,
                string.Join(",", reference.Item1.Activity!.Quantities.Select(quantity => quantity.Field))))
            .Select(group => group.Last()).ToList();

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
