using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies a status and publishes its committed relationships and consequences.</summary>
internal static class StatusAffliction
{
    internal static Card? Apply(
        World world, ICardFacts facts, Card host, string status,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(events);

        var given = Statuses.Inflict(world, facts, host, status);
        if (given is null)
        {
            return null;
        }

        events.Add(new CardsCreated(
            Places.Reference(given.Area), [new CreatedCard(given.ObjectId, given.FaceId)])
        {
            Trigger = trigger,
            Verb = "Give_Status",
        });
        events.Add(new CardAttached(given.ObjectId, host.ObjectId)
        {
            Trigger = trigger,
            Verb = "Give_Status",
        });

        if (Statuses.Vulnerable(world, facts, host))
        {
            Discard.Card(world, host, trigger, events);
        }

        return given;
    }
}
