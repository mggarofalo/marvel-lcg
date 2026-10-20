using System.Collections.Immutable;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;

namespace Marvel.Cards.Run;

/// <summary>Transfers projected stunned and confused counts across a same-character stage change.</summary>
internal static class AbilityProjectedStatusInheritance
{
    private static readonly ImmutableArray<string> CarriedStatuses = [Statuses.Stunned, Statuses.Confused];

    internal static bool HasCarry(Card prior, Card next, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> counts)
    {
        if (!string.Equals(cast.World.Facts.Title(prior.FaceId), cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal)) return false;
        foreach (string status in CarriedStatuses)
            if (Count(prior, status, cast, counts) > 0) return true;
        return false;
    }

    internal static void Apply(Card prior, Card next, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> counts, HashSet<(int Card, string Status)> changes)
    {
        // rr:villain-defeat.3.2: same-character stages carry their status cards.
        foreach (string status in CarriedStatuses)
        {
            int count = Count(prior, status, cast, counts);
            TraceSetStatusCount(prior, status, 0, cast, counts, changes);
            TraceSetStatusCount(next, status, count, cast, counts, changes);
        }
    }

    private static int Count(Card card, string status, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> counts) =>
        counts.GetValueOrDefault((card.ObjectId, status), Statuses.Count(cast.World, card, status));
}
