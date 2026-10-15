using Godot;

namespace Marvel.Godot;

/// <summary>Moves source visuals between stable grid slots without moving their hit geometry.</summary>
internal sealed class SourceGridSlotMotion
{
    private readonly Dictionary<Control, CardControl> cards;
    private readonly Dictionary<CardControl, Vector2> previous = [];
    private readonly Dictionary<CardControl, Tween> motions = [];
    private readonly Func<bool> enabled;
    private CardControl? held;

    internal SourceGridSlotMotion(GridContainer grid, Dictionary<Control, CardControl> cards, Func<bool> enabled)
    {
        this.cards = cards;
        this.enabled = enabled;
        grid.SortChildren += Settle;
    }

    internal void Capture(CardControl? dragged)
    {
        held = dragged;
        previous.Clear();
        foreach (CardControl card in cards.Values.Where(InteractionControl.IsUsable))
        {
            if (card == held) continue;
            previous.Add(card, card.GlobalPosition);
            Stop(card);
        }
    }

    private void Settle()
    {
        foreach ((CardControl card, Vector2 global) in previous)
        {
            if (!InteractionControl.IsUsable(card) || card == held) continue;
            card.Position = enabled() ? global - card.GetParent<Control>().GlobalPosition : Vector2.Zero;
            if (card.Position.IsZeroApprox()) continue;
            Tween tween = card.CreateTween();
            motions[card] = tween;
            tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(card, "position", Vector2.Zero, 0.14);
            tween.Finished += () => motions.Remove(card);
        }
        previous.Clear();
    }

    private void Stop(CardControl card)
    {
        if (motions.Remove(card, out Tween? tween) && GodotObject.IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
    }
}
