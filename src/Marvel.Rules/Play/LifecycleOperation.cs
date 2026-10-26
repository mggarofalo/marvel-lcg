using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the Lifecycle procedure.</summary>
public sealed record LifecycleOperation(LifecycleKind Kind) : AgendaOperation
{
    /// <summary>The exact paid-event initiation, when this lifecycle belongs to one.</summary>
    public EventPlayInitiation? EventInitiation { get; init; }

    /// <summary>Reachable threat-removal fact supplied by the exact event interpreter.</summary>
    public bool? EventRemovesThreat { get; init; }

    /// <summary>The original card price retained through its entry response window.</summary>
    public PlayedCardPayment? Payment { get; init; }

    /// <inheritdoc/>
    public override string What => Kind switch
    {
        LifecycleKind.CardPlayed => Steps.CardPlayed,
        LifecycleKind.EventPlayed => Steps.EventPlayed,
        LifecycleKind.EventPlayInitiated => EventPlayTiming.Initiated,
        LifecycleKind.CardEntersPlay => Steps.CardEntersPlay,
        LifecycleKind.FormChanged => Steps.FormChanged,
        LifecycleKind.BasicRecoveryUsed => BasicPowerTiming.RecoveryUsed,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;
}
