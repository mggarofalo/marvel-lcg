using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Projects a card's entry before it changes the live board.</summary>
internal static class ContinuousBlankingEntry
{
    internal static (ContinuousBlankingTransition Transition, ContinuousEffectDeparture? Departure)
        Preflight(ContinuousEffects effects, Card card, Area destination)
    {
        var transition = new ContinuousBlankingTransition(effects);
        var from = card.Area;
        var orders = new[] { from, destination }.Distinct()
            .ToDictionary(area => area, area => area.Cards.ToArray());
        using var projection = new ProjectionScope(orders);
        from.Remove(card);
        destination.Replace(destination.Cards.Append(card).ToArray());
        card.ProjectTo(destination);
        return (transition, transition.Preflight());
    }

    internal static IDisposable? Begin(ContinuousEffectDeparture? departure) => departure?.Begin();

    internal static void Complete(ContinuousBlankingTransition transition,
        ContinuousEffectDeparture? departure, string trigger, List<GameEvent> events)
    {
        departure?.Complete(trigger, events);
        transition.RecheckLimits();
    }

}
