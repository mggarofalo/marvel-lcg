namespace Marvel.Rules.Play;

/// <summary>One encounter entry, including its actual completion receipt.</summary>
public sealed record EncounterEntryOperation(EncounterEntryRequest Entry, EffectCompletion Completion) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.CardEntersPlay;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.EncounterEntry;
}
