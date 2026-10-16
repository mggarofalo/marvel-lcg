using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the EnemyAttack procedure.</summary>
public sealed record EnemyAttackOperation(int Character = -1) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.Attack;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
