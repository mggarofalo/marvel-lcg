using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.Rules.Timing;

/// <summary>Matches effect conditions and expiry to their owning activation.</summary>
public static class ContinuousEffectConditions
{
    internal static ContinuousEffect BindCurrentActivation(
        this ContinuousEffects effects, ContinuousEffect effect) =>
        effect.ActivationId is null && effect.AwaitingActivation is null
            && IsCombatBound(effect) && effects.world.Activation is { } activation
            ? effect with { ActivationId = activation.Id } : effect;

    private static bool IsCombatBound(ContinuousEffect effect) =>
        effect.Lasts?.Until is TimingPoints.EndOfAttack or TimingPoints.EndOfActivation
            || effect.Lasts?.OnCondition == Steps.AttackEnds;

    /// <summary>Bind pre-activation instructions to the activations their ability scheduled.</summary>
    public static void BindScheduledActivations(this ContinuousEffects effects,
        int occurrence, int source, IReadOnlyList<int> activations)
    {
        var pending = effects.entries.Where(entry =>
            entry.Effect.AwaitingActivation == occurrence && entry.Effect.Card == source).ToArray();
        foreach (var entry in pending)
        {
            effects.entries.Remove(entry);
            foreach (int activation in activations)
                effects.entries.Add(new ContinuousEffectEntry(entry.Effect with
                {
                    ActivationId = activation,
                    AwaitingActivation = null,
                }, entry.Origin));
        }
    }

    internal static int ExpireCore(this ContinuousEffects effects,
        string timingPoint, List<GameEvent>? events = null)
    {
        ArgumentNullException.ThrowIfNull(timingPoint);
        int? activation = effects.world.Activation?.Id;
        var expired = effects.entries.Where(entry =>
            entry.Effect.Lasts?.Until == timingPoint
            && Matches(entry.Effect, activation)).ToList();
        effects.End(expired, timingPoint, events);
        return expired.Count;
    }

    /// <summary>Retire an activation's effects even when its occurrence was replaced.</summary>
    public static void CompleteActivation(this ContinuousEffects effects,
        int activation, List<GameEvent> events)
    {
        // rr:lasting-effects.5: effects expire as soon as their stated duration ends.
        // The completion sentinel also runs when a replaced occurrence has no end window.
        var ending = effects.entries.Where(entry => entry.Effect.ActivationId == activation).ToList();
        effects.End(ending, TimingPoints.EndOfActivation, events);
    }

    internal static IReadOnlyList<ContinuousEffect> OccurCore(this ContinuousEffects effects,
        string condition, int? activationId)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var due = effects.entries.Where(entry => entry.Effect.Lasts?.OnCondition == condition
            && Matches(entry.Effect, activationId)).ToList();
        // rr:delayed-effect.1: resolve immediately at the specified condition. An
        // attack instruction applies to each damaged character, not only the first.
        var ending = due.Where(entry => !entry.Effect.RepeatsWithinDuration
            && entry.Remaining is null or <= 1).ToList();
        effects.End(ending, condition, events: null);
        foreach (var entry in due.Except(ending).Where(entry => entry.Remaining is not null))
            entry.Remaining -= 1;
        return [.. due.Select(entry => entry.Effect)];
    }

    private static bool Matches(ContinuousEffect effect, int? activation) =>
        effect.AwaitingActivation is null
        && (effect.ActivationId is null || effect.ActivationId == activation);
}
