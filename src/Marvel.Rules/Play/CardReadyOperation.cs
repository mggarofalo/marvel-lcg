using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A ready instruction with an observed would-ready interrupt window before it.</summary>
/// <param name="Batch">The cards readied and the replacements made to them.</param>
/// <param name="Completion">The containing ability's receipt, one part per card, or null.</param>
public sealed record CardReadyOperation(CardReadyBatch Batch, EffectCompletion? Completion) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => CardReadying.WouldReady;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Readying;

    internal Occurrence Occurrence(int id) => CardReadying.Occurrence(id, Batch);
}
