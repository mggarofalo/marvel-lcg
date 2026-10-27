using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Replaces one physical status without giving a status.</summary>
public static class StatusReplacement
{
    /// <summary>Physical status cards attached to live characters, in object-id order.</summary>
    public static IReadOnlyList<Card> Candidates(World world) =>
        [.. world.Cards.Where(card => Host(world, card) is not null).OrderBy(card => card.ObjectId)];

    /// <summary>Whether both the old status and its different replacement are legal.</summary>
    public static bool CanReplace(World world, Card status, string replacement) =>
        replacement is Statuses.Tough or Statuses.Stunned or Statuses.Confused
        && status.FaceId != replacement && Host(world, status) is { } host
        // rr:status-cards.1: "cannot have more than one status card of each type".
        && Statuses.Count(world, host, replacement) < Statuses.Limit(world, world.Facts, host, replacement);

    /// <summary>Commits both halves only after validating the complete replacement.</summary>
    public static bool Replace(World world, Card status, string replacement, string trigger, List<GameEvent> events)
    {
        if (!CanReplace(world, status, replacement)) return false;
        var host = Host(world, status)!;
        Discard.Card(world, status, trigger, events, "Replace_Status");
        var created = Statuses.Give(world, host, replacement);
        // ruling:0416488a6eb376e3: replacement does not trigger a given-status response.
        // These event verbs are engine choices, distinct from Give_Status.
        events.Add(new CardsCreated(Places.Reference(created.Area), [new CreatedCard(created.ObjectId, created.FaceId)])
            { Trigger = trigger, Verb = "Replace_Status" });
        events.Add(new CardAttached(created.ObjectId, host.ObjectId) { Trigger = trigger, Verb = "Replace_Status" });
        // rr:vulnerable.1: "when this character becomes confused or stunned".
        if (replacement is Statuses.Stunned or Statuses.Confused
            && Statuses.Afflicted(world, world.Facts, host, replacement)
            && Statuses.Vulnerable(world, world.Facts, host))
            Discard.Card(world, host, trigger, events);
        return true;
    }

    private static Card? Host(World world, Card status) =>
        status.Area.Type == DeckType.StatusArea
        && status.FaceId is Statuses.Tough or Statuses.Stunned or Statuses.Confused
        && status.Area.Host >= 0 && status.Area.Host < world.Cards.Count
        && world.Cards[status.Area.Host] is { } host && DeckTypes.IsInPlay(host.Area.Type)
        && CardKinds.IsCharacter(EffectiveCards.Kind(host, world.Facts)) ? host : null;
}
