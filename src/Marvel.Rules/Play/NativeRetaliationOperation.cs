namespace Marvel.Rules.Play;

/// <summary>Completes one native forced response after its damage children settle.</summary>
internal sealed record NativeRetaliationOperation(NativeRetaliationResolution Resolution) : AgendaOperation
{
    public override string What => "CompleteRetaliation";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
