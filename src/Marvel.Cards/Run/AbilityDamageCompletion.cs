using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Publishes one authored damage node's semantic result after all of its recipients finish.</summary>
internal sealed class AbilityDamageCompletion(Occurrence occurrence)
{
    private readonly List<AbilitySimultaneousDamageCompletion> batches = [];
    private readonly List<Direct> direct = [];
    private readonly List<DirectBatch> directBatches = [];
    private readonly List<Attack> attacks = [];
    private bool published;
    internal bool IsComplete => directBatches.All(item => item.Result.IsComplete) && batches.All(item => item.IsComplete) && direct.All(item => item.Result.IsComplete) && attacks.All(item => item.Result.IsComplete);
    internal void Add(AbilitySimultaneousDamageCompletion batch) => batches.Add(batch);
    internal void Add(SimultaneousDirectDamageResult result, long requested, bool marksDamage) =>
        directBatches.Add(new(result, requested, marksDamage));
    internal void Add(DamageResult result, long requested, bool marksDamage) => direct.Add(new(result, requested, marksDamage));
    internal void Add(AttackDamageResult result, long requested, Attacked? recipient = null,
        Occurrence? attackOccurrence = null) => attacks.Add(new(result, requested, recipient, attackOccurrence));

    internal System.Collections.Immutable.ImmutableArray<(int Card, int Incarnation)> Defeats =>
        // Engine choice: retain settled defeat identities for ordinary and staged
        // attacks alike. Recipient snapshots retain staged and resumed aftermath.
        [.. DirectResults().Where(item => item.Result.Outcome == Damage.Outcome.Defeated)
            .Select(item => (item.Result.Target, item.Result.TargetIncarnation))
            .Concat(attacks.SelectMany(item => item.Result.Execution.Occurrence.Defeats)
                .Select(item => (item.Card, item.Incarnation))).Distinct()];

    internal AbilityStructuralOutcome Complete()
    {
        if (!IsComplete) throw new InvalidOperationException("the damage node still owns unfinished recipients");
        AbilityStructuralOutcome? outcome = null;
        foreach (var item in DirectResults())
        {
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(item.Result.Dealt, item.Requested));
            if (item.MarksDamage && item.Result.Taken > 0) occurrence.Also(Steps.DamageDealt);
        }
        foreach (var item in attacks)
        {
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(item.Result.Dealt, item.Requested));
            if (item.Result.Characters.Count > 0) occurrence.Also(Steps.DamageDealt);
            PublishRecipientDamage(item);
        }
        foreach (var batch in batches)
            outcome = AbilityCompletion.Combine(outcome, batch.Complete());
        published = true;
        return outcome ?? AbilityStructuralOutcome.None;
    }

    private void PublishRecipientDamage(Attack item)
    {
        // rr:attack-player-ability-type.5: "attacked each of those enemies".
        // Snapshot facts survive defeat and departure; settlement supplies damage taken.
        if (published || item.Recipient is not { } recipient) return;
        var attack = item.Occurrence ?? throw new InvalidOperationException("A retained recipient has no attack occurrence.");
        attack.Also(recipient with { DamageTaken = item.Result.Taken });
        if (item.Result.Taken > 0) attack.Also(Steps.DamageDealt);
    }

    private IEnumerable<Direct> DirectResults() => direct.Concat(directBatches.SelectMany(batch =>
        batch.Result.Recipients.Select(result => new Direct(result, batch.Requested, batch.MarksDamage))));

    private sealed record DirectBatch(SimultaneousDirectDamageResult Result, long Requested, bool MarksDamage);
    private sealed record Direct(DamageResult Result, long Requested, bool MarksDamage);
    private sealed record Attack(AttackDamageResult Result, long Requested, Attacked? Recipient, Occurrence? Occurrence);
}
