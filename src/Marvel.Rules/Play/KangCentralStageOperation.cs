using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One central scheme reveal retained before its exact phase owner.</summary>
public sealed record KangCentralStageOperation(PhaseStep Owner, Occurrence Origin, bool Flip) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Flip ? "FlipKangCentralScheme" : "RevealKangCentralScheme";
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
