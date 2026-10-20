using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Publishes frozen recipient facts on the actual attack after its shared damage settles.</summary>
internal sealed class AbilitySimultaneousDamageCompletion(
    SimultaneousAttackDamageResult result, IReadOnlyList<Attacked> recipients,
    long requested, Occurrence occurrence)
{
    private bool published;
    internal bool IsComplete => result.IsComplete;

    internal AbilityStructuralOutcome Complete()
    {
        if (!IsComplete) throw new InvalidOperationException("simultaneous recipients have unfinished damage");
        AbilityStructuralOutcome? outcome = null;
        for (int index = 0; index < recipients.Count; index++)
        {
            var damage = result.Recipients[index];
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(damage.Dealt, requested));
            if (published) continue;
            occurrence.Also(recipients[index] with { DamageTaken = damage.Taken });
            if (damage.Taken > 0) occurrence.Also(Steps.DamageDealt);
        }
        published = true;
        return outcome ?? AbilityStructuralOutcome.None;
    }
}
