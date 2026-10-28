using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Local delayed effects sharing one retained Kang phase boundary.</summary>
public sealed record KangDepartureOrderOperation(
    IReadOnlyList<KangDeparture> Remaining, PhaseStep Owner, Occurrence Origin) : AgendaOperation
{
    /// <summary>Engine-chosen name of the simultaneous-departure ordering decision.</summary>
    public const string Order = "OrderKangPhaseDepartures";
    /// <inheritdoc/>
    public override string What => Order;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
