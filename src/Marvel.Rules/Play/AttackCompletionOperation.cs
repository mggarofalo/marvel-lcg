using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the AttackCompletion procedure.</summary>
public sealed record AttackCompletionOperation(Occurrence? Owner = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.EndAttack;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
