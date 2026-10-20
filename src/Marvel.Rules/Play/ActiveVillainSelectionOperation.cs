namespace Marvel.Rules.Play;

/// <summary>Suspends a tied active-counter selection inside its causing occurrence.</summary>
public sealed record ActiveVillainSelectionOperation(string Trigger) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => "ChooseActiveVillain";
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Defeat;
}
