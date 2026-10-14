using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Moves existing card surfaces around their reserved slots and lands exact receipt values at impact.</summary>
internal sealed class EventActivityCardMotion(Main main)
{
    private readonly Dictionary<CardControl, (Vector2 Position, float Rotation, Color Tint)> originals = [];
    private readonly Dictionary<CardControl, (Vector2 Position, float Rotation, bool Depart)> targets = [];
    private readonly Dictionary<CardControl, (Vector2 Position, float Rotation)> starts = [];
    private readonly EventActivityArrivals arrivals = new(main);
    private readonly Dictionary<int, BoardCardPresentation> displayed = [];
    private readonly HashSet<int> departed = [];
    private BoardRenderResult? boundBoard;
    private EventPresentation? current;
    private bool impacted;

    internal void Begin(EventPresentation? entry)
    {
        FinishBeat();
        current = entry;
        arrivals.Begin(entry);
        impacted = false;
        Bind(entry);
    }

    private void Bind(EventPresentation? entry)
    {
        boundBoard = main.boardRender;
        if (entry is null || boundBoard is not { } board) return;
        foreach (EventPresentation fact in EventActivityCardFacts.Facts(entry))
        {
            if (fact.Activity is not { } activity || activity.Operation == "Attack_Completed") continue;
            foreach (ActivitySubject subject in activity.Subjects)
            {
                if (board.ControlFor(subject.Card) is not CardControl card) continue;
                BindCard(card, subject, fact, activity);
            }
        }
    }

    private void BindCard(CardControl card, ActivitySubject subject, EventPresentation fact, EventActivityDescriptor activity)
    {
        originals.TryAdd(card, (card.Position, card.Rotation, card.Modulate));
        bool depart = subject.Role == ActivitySubjectRole.Subject
            && (activity.Operation == "Discard" || fact.Motion == EventMotionKind.Move);
        var pose = ReadinessPose(card, activity);
        starts.TryAdd(card, (card.Position, card.Rotation));
        targets[card] = (pose.Position, pose.Rotation, depart || (targets.TryGetValue(card, out var prior) && prior.Depart));
    }

    private static (Vector2 Position, float Rotation) ReadinessPose(CardControl card, EventActivityDescriptor activity)
    {
        ActivityQuantity? ready = activity.Quantities.FirstOrDefault(q => q.Field == "is_exhaust");
        if (ready?.After is null && activity.Operation != "Ready") return (card.Position, card.Rotation);
        return Pose(card, ready?.After == 1 ? "EXHAUSTED" : "READY");
    }

    private static (Vector2 Position, float Rotation) Pose(CardControl card, string status)
    {
        if (card.HasMeta("source_tableau_tile") || status == card.DisplayedPresentation?.Status)
            return (card.Position, card.Rotation);
        bool exhausted = status == "EXHAUSTED";
        return (card.Position + new Vector2(exhausted ? card.Size.Y : -card.Size.Y, 0), exhausted ? Mathf.Pi / 2 : 0);
    }

    internal void Advance(double elapsed, double onset)
    {
        if (!ReferenceEquals(boundBoard, main.boardRender)) Rebind();
        arrivals.Advance(elapsed, onset);
        float approach = Mathf.Clamp((float)(elapsed / onset), 0, 1);
        float settle = Mathf.Clamp((float)((elapsed - onset) / .18), 0, 1);
        float ease = 1 - Mathf.Pow(1 - approach, 3);
        foreach (var (card, target) in targets)
        {
            if (!InteractionControl.IsUsable(card)) continue;
            var origin = starts[card];
            Vector2 offset = target.Depart ? new Vector2(0, -26 * ease) : new Vector2(0, -4 * Mathf.Sin(approach * Mathf.Pi));
            card.Position = origin.Position.Lerp(target.Position, ease) + offset;
            card.Rotation = Mathf.Lerp(origin.Rotation, target.Rotation, ease);
            card.Modulate = target.Depart ? originals[card].Tint with { A = 1 - ease } : originals[card].Tint;
            if (!target.Depart && elapsed >= onset)
                card.Scale = Vector2.One * (1 + .018f * (1 - settle));
            SpatialCardSidecar.Place(card);
        }
        if (!impacted && elapsed >= onset)
        {
            impacted = true;
            Impact();
        }
    }

    private void Impact()
    {
        if (current is null || main.boardRender is not { } board) return;
        foreach (EventPresentation fact in EventActivityCardFacts.Facts(current))
            if (fact.Activity is { } activity) ApplyFacts(board, activity);
        foreach (var (card, target) in targets)
            if (InteractionControl.IsUsable(card) && target.Depart)
            {
                card.SetMeta("activity_departed", true);
                if (card.TargetId is { } id) departed.Add(id);
            }
        main.SetMeta("activity_impact", true);
    }

    private void ApplyFacts(BoardRenderResult board, EventActivityDescriptor activity)
    {
        foreach (ActivitySubject subject in activity.Subjects)
        {
            if (board.ControlFor(subject.Card) is not CardControl card || card.DisplayedPresentation is not { } shown) continue;
            EventActivityCardRendering.Apply(card, EventActivityCardFacts.Apply(shown, activity));
            displayed[subject.Card] = card.DisplayedPresentation!;
            if (main.cardInspector.Visible && main.inspectedCardId == subject.Card)
                main.boardController.ShowCardInspector(card.DisplayedPresentation!, card, main.cardInspectorPinned);
        }
    }

    private void FinishBeat()
    {
        if (current is not null && !impacted) Impact();
        foreach (var (card, target) in targets)
        {
            if (!InteractionControl.IsUsable(card)) continue;
            card.Position = target.Position;
            card.Rotation = target.Rotation;
            card.Scale = Vector2.One;
            if (target.Depart) card.Modulate = card.Modulate with { A = 0 };
            SpatialCardSidecar.Place(card);
        }
        targets.Clear();
        starts.Clear();
        main.SetMeta("activity_impact", false);
    }

    internal void Clear()
    {
        foreach (var (card, original) in originals)
        {
            if (!InteractionControl.IsUsable(card)) continue;
            card.Position = original.Position;
            card.Rotation = original.Rotation;
            card.Modulate = original.Tint;
            card.Scale = Vector2.One;
        }
        targets.Clear();
        starts.Clear();
        originals.Clear();
        displayed.Clear();
        departed.Clear();
        arrivals.Clear();
        current = null;
        impacted = false;
    }

    internal void Release()
    {
        targets.Clear();
        starts.Clear();
        originals.Clear();
        displayed.Clear();
        departed.Clear();
        arrivals.Clear();
        current = null;
        impacted = false;
    }

    private void Rebind()
    {
        targets.Clear();
        starts.Clear();
        if (main.boardRender is { } board)
            foreach (CardControl card in board.VisibleCardControls())
                RestoreCard(card);

        Bind(current);
        arrivals.Begin(current);
    }

    private void RestoreCard(CardControl card)
    {
        if (card.TargetId is not { } id) return;
        if (displayed.TryGetValue(id, out BoardCardPresentation? shown))
        {
            var pose = Pose(card, shown.Status);
            card.Position = pose.Position;
            card.Rotation = pose.Rotation;
            EventActivityCardRendering.Apply(card, shown);
        }
        if (departed.Contains(id))
        {
            card.Modulate = card.Modulate with { A = 0 };
            card.SetMeta("activity_departed", true);
        }
    }
}
