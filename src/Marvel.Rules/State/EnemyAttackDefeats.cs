using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>Exact damage causes and their established defeats for one enemy attack.</summary>
internal sealed class EnemyAttackDefeats
{
    private readonly List<(Occurrence Owner, int Card, int Incarnation)> causes = [];
    private readonly List<Defeated> defeats = [];

    internal IReadOnlyList<Defeated> Items => defeats;

    internal void Register(Occurrence owner, Card recipient) =>
        causes.Add((owner, recipient.ObjectId, recipient.Incarnation));

    internal bool Record(Occurrence owner, Defeated defeat)
    {
        // rr:triggering-condition.2: "a single attack causing a character to both
        // take damage and be defeated". Exact causal ownership, not a matching
        // damage total or a card's current zone, establishes that relationship.
        if (!Owns(owner, defeat)) return false;
        if (!defeats.Any(item => item.Card == defeat.Card && item.Incarnation == defeat.Incarnation))
            defeats.Add(defeat);
        return true;
    }

    private bool Owns(Occurrence owner, Defeated defeat) => causes.Any(cause =>
        ReferenceEquals(cause.Owner, owner) && cause.Card == defeat.Card && cause.Incarnation == defeat.Incarnation);
}
