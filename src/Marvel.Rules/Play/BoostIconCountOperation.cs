using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A boost-icon count with an observed interrupt window before it completes.</summary>
/// <param name="Count">The count, which interrupts may replace or adjust.</param>
public sealed record BoostIconCountOperation(BoostIconCount Count) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => BoostIconCounting.Counted;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.BoostCount;

    internal Occurrence Occurrence(int id, int player) => BoostIconCounting.Occurrence(id, Count, player);
}
