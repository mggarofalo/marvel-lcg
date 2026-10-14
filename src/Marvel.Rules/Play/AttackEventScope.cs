using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Records the engine's attack ownership around one agenda operation.</summary>
internal sealed class AttackEventScope(World world, List<GameEvent> events) : IDisposable
{
    private readonly ActiveAttack? before = ActiveAttack.From(world);
    private readonly ActiveAttack[] openBefore = ActiveAttack.Open(world).DistinctBy(attack => attack.Identity).ToArray();
    private readonly int start = events.Count;
    private readonly EventCause? cause = EventCauses.From(world);

    public void Dispose()
    {
        ActiveAttack? after = ActiveAttack.From(world);
        AttributeEvents(after);
        FinishOwners();
        StartOwner(after);
    }

    private void AttributeEvents(ActiveAttack? after)
    {
        EventCause? causalOwner = cause ?? EventCauses.From(world);
        var identity = (before ?? after)?.Identity;
        for (int index = start; index < events.Count; index++)
            events[index] = events[index] with
            {
                Cause = events[index].Cause ?? causalOwner,
                AttackIdentity = events[index].AttackIdentity ?? identity,
            };
    }

    private void FinishOwners()
    {
        HashSet<AttackIdentity?> stillOpen = ActiveAttack.Open(world).Select(attack => attack.Identity).ToHashSet();
        foreach (ActiveAttack ended in openBefore.Where(attack => attack.Identity is not null && !stillOpen.Contains(attack.Identity)))
            events.Add(Boundary(ended, false));
    }

    private void StartOwner(ActiveAttack? after)
    {
        if (before?.Identity != after?.Identity && after?.Identity is not null)
        {
            if (openBefore.Any(attack => attack.Identity == after.Identity)) events.Add(Boundary(after, true));
            else InsertStart(after);
        }
        else if (before is not null && after is not null && before.Target != after.Target)
            events.Add(Boundary(after, true));
    }

    private void InsertStart(ActiveAttack attack)
    {
        // Activity batches are offsets in this event stream. An inserted semantic
        // onset rebases receipts and members together, without changing resolution.
        for (int index = 0; index < events.Count; index++)
        {
            GameEvent entry = events[index];
            if (entry is ActivityCompleted completed && completed.Batch >= start)
                entry = completed with { Batch = completed.Batch + 1 };
            if (entry.ActivityBatch is { } batch && batch >= start)
                entry = entry with { ActivityBatch = batch + 1 };
            events[index] = entry;
        }
        events.Insert(start, Boundary(attack, true) with { Resumed = false });
    }

    private AttackBoundary Boundary(ActiveAttack attack, bool started) =>
        new(attack.Identity!, attack.Source, attack.Target, started)
        {
            AttackIdentity = attack.Identity,
            Resumed = started && openBefore.Any(prior => prior.Identity == attack.Identity),
            Trigger = started ? Steps.AttackInitiated : Steps.AttackEnds,
            Verb = started ? "Attack_Initiated" : "Attack_Finished",
            Subjects = new[] { attack.Source, attack.Target }.Distinct().ToDictionary(id => id,
                id => AttackCompletion.SubjectTitle(world, world.Facts, id)),
        };
}
