using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>A preflighted set of state-based changes after constants end.</summary>
internal sealed class ContinuousEffectDeparture
{
    private readonly ContinuousEffects effects;
    private readonly IReadOnlyList<Card> restored;
    private readonly IReadOnlyList<int> departures;
    private bool completed;

    internal ContinuousEffectDeparture(
        ContinuousEffects effects,
        IReadOnlyList<Card> restored,
        IReadOnlyList<int> departures)
    {
        this.effects = effects;
        this.restored = restored;
        this.departures = departures;
    }

    /// <summary>
    /// Mark the whole preflighted cascade as one departure while it is applied.
    /// </summary>
    public IDisposable Begin() => effects.BeginDepartures(departures);

    /// <summary>Apply the preflighted changes after the source has left play.</summary>
    public void Complete(string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);
        if (completed)
        {
            return;
        }

        effects.CompleteConstantsEnding(restored, trigger, events);
        completed = true;
    }
}
