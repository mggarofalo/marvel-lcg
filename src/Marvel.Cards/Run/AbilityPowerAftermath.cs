using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Retains a labelled attack's place while retaliation resolves nested choices.</summary>
internal sealed class AbilityPowerAftermath
{
    private readonly Card[] targets;
    private readonly Card attacker;
    private readonly Dictionary<int, Attacked> recipients;
    private int next;
    private DamageResult? pending;

    internal AbilityPowerAftermath(AbilityResolutionState cast, string power)
    {
        recipients = (cast.PowerExecution?.Occurrence?.Attacks ?? [])
            .ToDictionary(recipient => recipient.Card);
        attacker = cast.PowerActor ?? cast.World.Seats[cast.Player].IdentityCard;
        targets = power == BasicPowers.AttackVerb
            && !AttackKeywords.Has(cast.World, attacker, cast.Source, Keywords.Ranged)
            ? [.. cast.Attacked.DistinctBy(card => card.ObjectId)] : [];
    }

    internal bool Complete(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (pending is { IsComplete: false })
            throw new InvalidOperationException("Attack aftermath resumed before its damage completed.");
        pending = null;
        while (next < targets.Length)
        {
            var target = targets[next++];
            if (!IsCurrentRecipient(target)) continue;
            var owner = cast.World.Agenda.Current;
            var occurrence = cast.World.Agenda.Occurrence;
            pending = DamageAttacks.Retaliate(cast.World, cast.World.Facts,
                target, attacker, cast.Trigger, cast.Events);
            if (pending is not { IsComplete: false }) continue;

            // rr:interrupt.3: an interrupt resolves before its triggering condition.
            // Keep the containing power suspended until that damage execution settles.
            var capture = execution.Capture(cast, cast.PowerExecution!.Decoded.Ordinal) with
            { Reason = AbilityResumeReason.PowerAftermath };
            var continuation = AbilityContinuations.Step(capture,
                AbilityContinuationStage.Resume, cast.World.Agenda.Current?.Round ?? 0, plan: true);
            if (owner is null) cast.World.Agenda.Then(continuation);
            else cast.World.Agenda.ContinueBeforeOwner(
                occurrence ?? throw new InvalidOperationException("Attack aftermath has no occurrence."),
                owner.Value, continuation);
            cast.Suspend();
            cast.CompleteResolution();
            return false;
        }
        return true;
    }

    private bool IsCurrentRecipient(Card card)
    {
        if (!recipients.TryGetValue(card.ObjectId, out var recipient)) return true;
        // rr:leaves-play.1 prevents a new copy retaliating for the old copy.
        // Departure preserves this incarnation until reentry. The shared
        // retaliation procedure can therefore resolve the next physical stage
        // of the same villain (rr:villain-defeat.3.1).
        return card.Incarnation == recipient.Incarnation;
    }

}
