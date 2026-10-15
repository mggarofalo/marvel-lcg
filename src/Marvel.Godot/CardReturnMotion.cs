using Godot;

namespace Marvel.Godot;

/// <summary>Owns cancellable card settling so a fresh grab cannot race an old return.</summary>
internal sealed class CardReturnMotion
{
    private readonly Dictionary<Control, Tween> motions = [];

    internal void Stop(Control card)
    {
        if (!motions.Remove(card, out Tween? tween)) return;
        if (GodotObject.IsInstanceValid(tween) && tween.IsValid()) tween.Kill();
        MarkInactive(card);
    }

    internal void Settle(Control card, Vector2 position, float rotation, Action finished)
    {
        Stop(card);
        double duration = .22 + .06 * Math.Min(1, card.Position.DistanceTo(position) / 600);
        Tween tween = card.CreateTween().SetParallel();
        motions.Add(card, tween);
        card.SetMeta("card_return_active", true);
        tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "position", position, duration);
        tween.TweenProperty(card, "rotation", rotation, duration);
        tween.TweenProperty(card, "modulate", Colors.White, duration);
        tween.Finished += () =>
        {
            if (!motions.TryGetValue(card, out Tween? current) || !ReferenceEquals(current, tween)) return;
            motions.Remove(card);
            if (InteractionControl.IsUsable(card))
            {
                finished();
                card.ForceUpdateTransform();
            }
            if (!motions.ContainsKey(card)) MarkInactive(card);
        };
    }

    private static void MarkInactive(Control card)
    {
        if (GodotObject.IsInstanceValid(card)) card.SetMeta("card_return_active", false);
    }
}
