using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the PhaseTransition procedure.</summary>
public sealed record PhaseTransitionOperation(PhaseTransitionKind Kind) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        PhaseTransitionKind.PassFirstPlayerToken => Steps.PassFirstPlayerToken,
        PhaseTransitionKind.EndVillainPhase => Steps.EndVillainPhase,
        PhaseTransitionKind.DrawToHandSize => Steps.DrawToHandSize,
        PhaseTransitionKind.ReadyCards => Steps.ReadyCards,
        PhaseTransitionKind.EndPlayerPhase => Steps.EndPlayerPhase,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
