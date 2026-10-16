using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Publishes one authored damage node's semantic result after all of its recipients finish.</summary>
internal sealed class AbilityDamageCompletion(Occurrence occurrence)
{
    private readonly List<Direct> direct = [];
    private readonly List<Attack> attacks = [];
    internal bool IsComplete => direct.All(item => item.Result.IsComplete) && attacks.All(item => item.Result.IsComplete);
    internal void Add(DamageResult result, long requested, bool marksDamage) => direct.Add(new(result, requested, marksDamage));
    internal void Add(AttackDamageResult result, long requested) => attacks.Add(new(result, requested));

    internal AbilityStructuralOutcome Complete()
    {
        if (!IsComplete) throw new InvalidOperationException("the damage node still owns unfinished recipients");
        AbilityStructuralOutcome? outcome = null;
        foreach (var item in direct)
        {
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(item.Result.Dealt, item.Requested));
            if (item.MarksDamage && item.Result.Taken > 0) occurrence.Also(Steps.DamageDealt);
        }
        foreach (var item in attacks)
        {
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(item.Result.Dealt, item.Requested));
            if (item.Result.Characters.Count > 0) occurrence.Also(Steps.DamageDealt);
        }
        return outcome ?? AbilityStructuralOutcome.None;
    }

    private sealed record Direct(DamageResult Result, long Requested, bool MarksDamage);
    private sealed record Attack(AttackDamageResult Result, long Requested);
}
