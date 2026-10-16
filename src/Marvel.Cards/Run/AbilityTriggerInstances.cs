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
        // rr:triggering-condition.1-.2: shared windows retain distinct instances
        // of a triggering condition. All clauses must describe the same defeat.
        if (trigger.Event != Steps.CardDefeated && trigger.Also != Steps.CardDefeated)
        {
            yield return candidate;
            yield break;
        }
        foreach (var defeat in occurrence.Defeats)
            yield return candidate with { TriggeringDefeat = defeat };
    }
}
