namespace Marvel.Rules.Play;

/// <summary>Closed, operation-specific state for one scheduled rules procedure.</summary>
/// <remarks>Payload shape is an engine choice; rules determine resolution and timing.</remarks>
public abstract record AgendaOperation
{
    private protected AgendaOperation() { }
    /// <summary>The timing vocabulary name of this operation.</summary>
    public abstract string What { get; }
    /// <summary>The rules procedure responsible for this operation.</summary>
    public AgendaProcedureKind Procedure => ProcedureOwner;

    // A private protected abstract member closes concrete derivation even through
    // the protected copy constructor synthesized for an abstract record.
    private protected abstract AgendaProcedureKind ProcedureOwner { get; }
}
