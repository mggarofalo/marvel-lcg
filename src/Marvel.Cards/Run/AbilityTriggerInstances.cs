using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Binds a complete triggering fact before evaluating an ability's predicates.</summary>
internal static class AbilityTriggerInstances
{
    internal static IEnumerable<AbilityWindowAdmission.Candidate> For(
        AbilityWindowAdmission.Candidate candidate, Occurrence occurrence)
    {
        var trigger = candidate.Ability.Trigger;
        bool targetedAttack = trigger.Target is not null
            && (trigger.Event == Steps.AttackEnds || trigger.Also == Steps.AttackEnds)
            && occurrence.Attacks.Count > 0;
        foreach (var instance in DefeatInstances(candidate, occurrence))
        {
            if (!targetedAttack) { yield return instance; continue; }
            foreach (var attack in occurrence.Attacks)
                if (Matches(instance, attack)) yield return instance with { TriggeringAttack = attack };
        }
    }

    private static bool Matches(AbilityWindowAdmission.Candidate candidate, Marvel.Rules.State.Attacked attack)
    {
        // rr:triggering-condition.1-.2: all clauses describe the same instance,
        // even though several instances share one response window.
        if (candidate.TriggeringDefeat is { } defeat
            && (defeat.Card != attack.Card || defeat.Incarnation != attack.Incarnation)) return false;
        var trigger = candidate.Ability.Trigger;
        return trigger.Event != Steps.DamageDealt && trigger.Also != Steps.DamageDealt
            || attack.DamageTaken > 0;
    }

    private static IEnumerable<AbilityWindowAdmission.Candidate> DefeatInstances(
        AbilityWindowAdmission.Candidate candidate, Occurrence occurrence)
    {
        var trigger = candidate.Ability.Trigger;
        if (trigger.Event != Steps.CardDefeated && trigger.Also != Steps.CardDefeated)
        {
            yield return candidate;
            yield break;
        }
        foreach (var defeat in occurrence.Defeats)
            yield return candidate with { TriggeringDefeat = defeat };
    }
}
