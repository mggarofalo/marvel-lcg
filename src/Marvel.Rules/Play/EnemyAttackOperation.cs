using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the EnemyAttack procedure.</summary>
public sealed record EnemyAttackOperation(int Character = -1) : AgendaOperation
{
    /// <summary>Whether this attack may receive new boost cards.</summary>
    public bool DealBoostCards { get; init; } = true;

    /// <inheritdoc/>
    public override string What => Steps.Attack;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
