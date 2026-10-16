using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ActivationCompletion procedure.</summary>
public sealed record ActivationCompletionOperation(ActivationCompletionKind Kind) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        ActivationCompletionKind.CompleteAttackActivation => Steps.CompleteAttackActivation,
        ActivationCompletionKind.CompleteSchemeActivation => Steps.CompleteSchemeActivation,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
