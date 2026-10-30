namespace Marvel.Rules.Play;

/// <summary>One physical scheme reveal or its subsequent flip in the first split.</summary>
public sealed record KangSplitOperation(bool Flip = false, IReadOnlyList<int>? Unused = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Flip ? "FlipKangSplitScheme" : "RevealKangSplitScheme";
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
