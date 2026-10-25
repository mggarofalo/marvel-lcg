namespace Marvel.Rules.Play;

/// <summary>The response window of a card effect whose immediate changes have applied.</summary>
public sealed record CompletedCardEffectOperation : AgendaOperation
{
    /// <summary>An explicitly owned response occurrence, when supplied by a batch.</summary>
    public Timing.Occurrence? Occurrence { get; init; }
    /// <inheritdoc/>
    public override string What => Steps.CardEffectResolved;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;
}
