using Godot;

namespace Marvel.Godot;

/// <summary>Reversible local hand poses for insertion and opening-hand staging.</summary>
internal sealed class HandGestureLayout(Action<Control> poseChanged)
{
    private readonly List<CardControl> cards = [];
    private readonly Dictionary<CardControl, (Vector2 Position, float Rotation, int Z)> slots = [];
    private readonly Dictionary<CardControl, Tween> motions = [];
    private readonly Dictionary<CardControl, string> descriptions = [];
    private readonly HashSet<int> staged = [];
    private Control? area;
    private Control? stage;
    private int seat;
    private List<CardControl>? preview;
    private bool opening;
    internal Func<int, int, float, SpatialCardPlacement>? Placement { get; set; }
    internal Func<bool>? MotionEnabled { get; set; }

    internal void RegisterArea(Control control, int player) { area = control; seat = player; }
    internal void RegisterStage(Control control) => stage = control;
    internal void Track(CardControl card)
    {
        if (cards.Contains(card)) return;
        cards.Add(card);
        descriptions[card] = card.TooltipText;
        card.TreeExiting += () =>
        {
            StopMotion(card);
            cards.Remove(card);
            slots.Remove(card);
            descriptions.Remove(card);
        };
    }
    internal void Capture(Control card)
    {
        if (card is CardControl hand && cards.Contains(hand))
            slots[hand] = (hand.Position, hand.Rotation, hand.ZIndex);
    }

    internal bool Contains(Vector2 position) => InteractionControl.IsUsable(area)
        && area!.GetGlobalRect().HasPoint(position);

    internal bool IsStaged(int id) => staged.Contains(id);

    internal void Stage(IReadOnlyCollection<int> selected)
    {
        opening = true;
        staged.Clear();
        staged.UnionWith(selected);
        ApplyStaging();
    }

    internal void Preview(Control source, Vector2 position)
    {
        if (source is not CardControl dragged || !CanPreview(dragged, position))
        {
            CancelPreview();
            return;
        }
        var remaining = cards.Where(card => card != dragged && !Staged(card)).ToList();
        int insertion = remaining.Count(card => card.GetParent() is Control parent
            && (parent.GetGlobalTransform() * slots[card].Position).X + card.Size.X / 2 < position.X);
        remaining.Insert(insertion, dragged);
        if (preview is not null && preview.SequenceEqual(remaining)) return;
        preview = remaining;
        var orderedSlots = cards.Where(card => !Staged(card)).Select(card => slots[card]).ToArray();
        for (int index = 0; index < remaining.Count; index++)
            if (remaining[index] != dragged) Move(remaining[index], orderedSlots[index], false);
    }

    private bool CanPreview(CardControl card, Vector2 position) =>
        cards.Contains(card) && Contains(position) && !Staged(card);

    internal bool Drop(Control source, Vector2 position)
    {
        if (source is not CardControl card || !Contains(position) || preview is null
            || !preview.Contains(card)) return false;
        var order = preview;
        var orderedSlots = cards.Where(candidate => !Staged(candidate)).Select(candidate => slots[candidate]).ToArray();
        // Presentation chooses a stable master order: staged cards reserve their slots.
        int keptIndex = 0;
        for (int index = 0; index < cards.Count; index++)
            if (!Staged(cards[index])) cards[index] = order[keptIndex++];
        for (int index = 0; index < order.Count; index++)
        {
            slots[order[index]] = orderedSlots[index];
            Move(order[index], orderedSlots[index], true);
        }
        HandPresentationOrder.Store(seat, cards.Where(candidate => candidate.TargetId is not null)
            .Select(candidate => candidate.TargetId!.Value));
        preview = null;
        return true;
    }

    internal void CancelPreview()
    {
        if (preview is null) return;
        preview = null;
        ApplyStaging();
    }

    private bool Staged(CardControl card) => card.TargetId is { } id && staged.Contains(id);

    private void ApplyStaging()
    {
        ReflowKeptCards();
        int index = 0;
        foreach (CardControl card in cards.Where(InteractionControl.IsUsable))
        {
            if (!slots.TryGetValue(card, out var pose)) continue;
            if (Staged(card) && InteractionControl.IsUsable(stage) && card.GetParent() is Control parent)
            {
                Vector2 location = parent.GetGlobalTransform().AffineInverse()
                    * (stage!.GlobalPosition + new Vector2(index++ * 34, 28));
                Move(card, (location, 0, 170 + index), true);
                card.SetMeta("mulligan_staged", true);
            }
            else
            {
                card.SetMeta("mulligan_staged", false);
                Move(card, pose, true);
            }
            if (opening) card.TooltipText = descriptions[card] + " · " + (Staged(card)
                ? "Staged for redraw. Drag back to hand or press Space to keep. Not discarded until Redraw."
                : "Drag to discard to stage a redraw, or press Space. Changes remain uncommitted.");
        }
    }

    private void ReflowKeptCards()
    {
        if (!opening || Placement is null) return;
        CardControl[] kept = [.. cards.Where(card => !Staged(card))];
        for (int index = 0; index < kept.Length; index++)
        {
            CardControl card = kept[index];
            SpatialCardPlacement pose = Placement(index, kept.Length, card.Size.X);
            slots[card] = (pose.Position, pose.Rotation, pose.ZIndex);
            float exposed = index + 1 == kept.Length ? card.Size.X
                : Math.Min(card.Size.X, Placement(index + 1, kept.Length, card.Size.X).Position.X - pose.Position.X);
            card.SetMeta("spatial_hand_exposed_width", exposed);
            card.SetMeta("spatial_hand_overlap", pose.Overlaps);
        }
    }

    private void Move(CardControl card, (Vector2 Position, float Rotation, int Z) pose, bool capture)
    {
        StopMotion(card);
        card.ZIndex = pose.Z;
        if (capture)
        {
            // Capture the final authored pose before the pointer return animation runs.
            card.Position = pose.Position;
            card.Rotation = pose.Rotation;
            poseChanged(card);
        }
        if (MotionEnabled?.Invoke() == false || capture)
        {
            card.Position = pose.Position;
            card.Rotation = pose.Rotation;
            SpatialHandActionStrip.Refresh(card);
            return;
        }
        Tween tween = card.CreateTween().SetParallel();
        motions[card] = tween;
        tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "position", pose.Position, 0.12);
        tween.TweenProperty(card, "rotation", pose.Rotation, 0.12);
        tween.Finished += () =>
        {
            if (motions.TryGetValue(card, out Tween? current) && ReferenceEquals(current, tween))
                motions.Remove(card);
        };
    }

    private void StopMotion(CardControl card)
    {
        if (motions.Remove(card, out Tween? existing)
            && GodotObject.IsInstanceValid(existing) && existing.IsValid()) existing.Kill();
    }
}
