namespace Marvel.Rules.Play;

/// <summary>Finish a card play after its accepted resource contributors finish.</summary>
internal sealed record CardPaymentEntryOperation(CardPaymentEntry Entry) : AgendaOperation
{
    public override string What => "CompleteCardPayment";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.ResourcePayment;
}
