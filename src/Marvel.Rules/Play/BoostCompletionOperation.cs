using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the BoostCompletion procedure.</summary>
public sealed record BoostCompletionOperation(int Enemy, bool Attacking) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.FinishBoostCard;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
