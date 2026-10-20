using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the Lifecycle procedure.</summary>
public sealed record LifecycleOperation(LifecycleKind Kind) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        LifecycleKind.CardPlayed => Steps.CardPlayed,
        LifecycleKind.EventPlayed => Steps.EventPlayed,
        LifecycleKind.CardEntersPlay => Steps.CardEntersPlay,
        LifecycleKind.FormChanged => Steps.FormChanged,
        LifecycleKind.BasicRecoveryUsed => BasicPowerTiming.RecoveryUsed,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;
}
