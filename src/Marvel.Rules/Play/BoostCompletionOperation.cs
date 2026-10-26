using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the BoostCompletion procedure.</summary>
public sealed record BoostCompletionOperation(int Enemy, bool Attacking) : AgendaOperation
{
    /// <summary>The exact per-card numeric contribution, when captured at faceup.</summary>
    public BoostIconCancellation? Icons { get; init; }
    /// <inheritdoc/>
    public override string What => Steps.FinishBoostCard;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
