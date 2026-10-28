using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Publishes frozen recipient facts on the actual attack after its shared damage settles.</summary>
internal sealed class AbilitySimultaneousDamageCompletion(
    SimultaneousAttackDamageResult? result, IReadOnlyList<Attacked> recipients, IReadOnlyList<int> resolvedTargets,
    long requested, Occurrence occurrence)
{
    private bool published;
    internal bool IsComplete => result is null || result.IsComplete;

    internal AbilityStructuralOutcome Complete()
    {
        if (!IsComplete) throw new InvalidOperationException("simultaneous recipients have unfinished damage");
        var completed = result?.Recipients ?? [];
        var damageByRecipient = resolvedTargets.Select((id, index) => (id, Damage: completed[index]))
            .ToDictionary(item => item.id, item => item.Damage);
        AbilityStructuralOutcome? outcome = null;
        foreach (var recipient in recipients)
        {
            var damage = damageByRecipient.GetValueOrDefault(recipient.Card);
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(damage?.Dealt ?? 0, requested));
            Publish(recipient, damage);
        }
        published = true;
        return outcome ?? AbilityStructuralOutcome.None;
    }

    private void Publish(Attacked recipient, AttackDamageResult? damage)
    {
        if (published) return;
        occurrence.Also(recipient with { DamageTaken = damage?.Taken ?? 0, ExcessDamage = damage?.Excess ?? 0 }, accumulate: true);
        if (damage is { Taken: > 0 }) occurrence.Also(Steps.DamageDealt);
    }

}
