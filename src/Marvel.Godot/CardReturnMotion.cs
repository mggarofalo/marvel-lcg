using Godot;

namespace Marvel.Godot;

/// <summary>Owns cancellable card settling so a fresh grab cannot race an old return.</summary>
internal sealed class CardReturnMotion
{
    private readonly Dictionary<Control, Tween> motions = [];

    internal void Stop(Control card)
    {
        if (motions.Remove(card, out Tween? tween)
            && GodotObject.IsInstanceValid(tween) && tween.IsValid()) tween.Kill();
    }

    internal void Settle(Control card, Vector2 position, float rotation, Action finished)
    {
        Stop(card);
        Tween tween = card.CreateTween().SetParallel();
        motions.Add(card, tween);
        tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "position", position, 0.16);
        tween.TweenProperty(card, "rotation", rotation, 0.16);
        tween.TweenProperty(card, "modulate", Colors.White, 0.16);
        tween.Finished += () =>
        {
            if (!motions.TryGetValue(card, out Tween? current) || !ReferenceEquals(current, tween)) return;
            motions.Remove(card);
            if (InteractionControl.IsUsable(card)) finished();
        };
    }
}
