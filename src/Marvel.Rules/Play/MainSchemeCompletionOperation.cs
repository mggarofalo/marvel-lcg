namespace Marvel.Rules.Play;

/// <summary>Retains the completing scheme until its interrupt window finishes.</summary>
public sealed record MainSchemeCompletionOperation(long Incarnation) : AgendaOperation
{
    /// <summary>The engine's name for the scheme completion triggering condition.</summary>
    public const string Completed = "WhenMainSchemeCompleted";

    /// <inheritdoc/>
    public override string What => Completed;

    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Threat;
}
