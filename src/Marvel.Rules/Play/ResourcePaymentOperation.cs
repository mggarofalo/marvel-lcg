namespace Marvel.Rules.Play;

/// <summary>Resume an admitted payment after its current generator finishes.</summary>
internal sealed record ResourcePaymentOperation(CardResourcePayment Payment) : AgendaOperation
{
    public override string What => "ContinueResourcePayment";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.ResourcePayment;
}
