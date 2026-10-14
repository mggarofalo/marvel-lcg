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
    private EventActivityPulse? attackPulse;
    private EventPresentation? attack;
    private double elapsed;
    private double attackElapsed;
    private bool suppressObjects;
    private EventActivityCardMotion? cards;
    internal event Action? Settled;

    internal void Configure(Main owner)
    {
        main = owner;
        cards = new EventActivityCardMotion(owner);
        pulse = new EventActivityPulse { Name = "EventActivityPulse", MouseFilter = Control.MouseFilterEnum.Ignore,
            TopLevel = true, ZAsRelative = false, ZIndex = 2000 };
        AddChild(pulse);
        attackPulse = new EventActivityPulse { Name = "AttackActivityPulse", MouseFilter = Control.MouseFilterEnum.Ignore,
            TopLevel = true, ZAsRelative = false, ZIndex = 1999 };
        AddChild(attackPulse);
        pulse.Exclusions = () => ActivityConnectorOcclusion.Regions(owner);
        attackPulse.Exclusions = pulse.Exclusions;
        SetProcess(false);
    }

    internal void PresentAttack(EventPresentation? active)
    {
        if (!Enumerable.SequenceEqual(attack?.Anchors ?? [], active?.Anchors ?? [])) attackElapsed = 0;
        attack = active;
        RefreshPulse();
        SetProcess(sequence.Active || attack is not null);
    }

    internal void Present(IReadOnlyList<EventPresentation> entries)
    {
        cards!.Clear();
        sequence.Start(entries, main!.eventMotion.ButtonPressed);
        cards.Begin(sequence.Attention);
        suppressObjects = false;
        elapsed = 0;
        host = null;
        RefreshHost();
        SetProcess(entries.Count > 0 || attack is not null);
        RefreshPulse();
    }

    public override void _Process(double delta)
    {
        if (!InteractionControl.IsUsable(main)) return;
        elapsed += delta;
        attackElapsed += delta;
        if (!main!.eventMotion.ButtonPressed && sequence.Active) Complete();
        cards!.Advance(sequence.Elapsed + delta, sequence.Onset);
        bool changed = sequence.Advance(delta);
        if (changed) BeatAdvanced();
        if (RefreshHost() || changed) UpdateText();
        RefreshPulse();
        if (!sequence.Active && attack is null) SetProcess(false);
    }

    private void BeatAdvanced()
    {
        elapsed = 0;
        cards!.Begin(sequence.Attention);
        if (!sequence.Active)
        {
            cards.Release();
            Settled?.Invoke();
        }
    }

    internal void Complete()
    {
        bool wasActive = sequence.Active;
        cards!.Clear();
        sequence.Complete();
        UpdateText();
        RefreshPulse();
        if (wasActive) Settled?.Invoke();
    }

    internal void SettleForDraft()
    {
        suppressObjects = true;
        Complete();
    }

    internal void Clear()
    {
        cards!.Clear();
        sequence.Clear();
        UpdateText();
        RefreshPulse();
        SetProcess(attack is not null);
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
        main?.eventController.PresentTimelineBeat(sequence);
    }

    private void RefreshPulse()
    {
        if (!InteractionControl.IsUsable(pulse)) return;
        var visible = sequence.Attention is { } current && !(attack is not null && current.Motion == EventMotionKind.Attack)
            ? VisibleSubjects([current]) : [];
        pulse!.Present(visible, elapsed, sequence.Active, sequence.Onset);
        attackPulse!.Present(attack is null ? [] : VisibleSubjects([attack], duringDraft: true),
            attackElapsed, main!.eventMotion.ButtonPressed);
    }

    private List<(EventPresentation, ActivitySubject, Rect2)> VisibleSubjects(IReadOnlyList<EventPresentation> entries,
        bool duringDraft = false)
    {
        List<(EventPresentation, ActivitySubject, Rect2)> visible = [];
        if ((!duringDraft && suppressObjects) || main!.boardRender is not { } board) return visible;
        foreach (EventPresentation entry in entries)
            foreach (ActivitySubject subject in entry.Activity?.Subjects ?? [])
                if (board.ControlFor(subject.Card) is { } card && VisibleBounds(card) is { } bounds)
                    visible.Add((entry, subject, bounds));
        return visible;
    }

    private static Rect2? VisibleBounds(Control card)
    {
        if (!InteractionControl.IsUsable(card) || !card.IsVisibleInTree() || card.HasMeta("activity_departed")) return null;
        Rect2 bounds = SpatialCardFootprint.Face(card).Intersection(card.GetViewportRect());
        for (Node? parent = card.GetParent(); parent is not null; parent = parent.GetParent())
            if (parent is Control clip && (clip.ClipContents || clip is ScrollContainer))
                bounds = bounds.Intersection(clip.GetGlobalRect());
        return bounds.HasArea() ? bounds : null;
    }
}
