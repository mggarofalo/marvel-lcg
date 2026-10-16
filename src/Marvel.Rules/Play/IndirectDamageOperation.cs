using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the IndirectDamage procedure.</summary>
public sealed record IndirectDamageOperation(IndirectDamageKind Kind,
    int Character = -1, int Source = -1, long Amount = 0,
    Occurrence? Origin = null, IReadOnlyList<int>? Candidates = null,
    IReadOnlyDictionary<int, long>? Amounts = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        IndirectDamageKind.AssignIndirectAttackDamage => Steps.AssignIndirectAttackDamage,
        IndirectDamageKind.PrepareIndirectAttackDamage => Steps.PrepareIndirectAttackDamage,
        IndirectDamageKind.ApplyIndirectAttackDamage => Steps.ApplyIndirectAttackDamage,
        IndirectDamageKind.FinishIndirectAttackDamage => Steps.FinishIndirectAttackDamage,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
