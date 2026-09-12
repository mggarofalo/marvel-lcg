using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A closed operation payload stored by one agenda step.</summary>
/// <remarks>
/// The operation vocabulary and its grouping are engine choices. Rules clauses
/// determine what each operation does and when it occurs; they do not prescribe
/// a software payload format.
/// </remarks>
public abstract record AgendaOperation
{
    private protected AgendaOperation(
        string what, AgendaProcedureKind procedure, AgendaOperationData data)
    {
        What = what;
        Procedure = procedure;
        Data = data;
    }

    /// <summary>The operation name used by timing conditions.</summary>
    public string What { get; }

    /// <summary>The procedure owner category.</summary>
    public AgendaProcedureKind Procedure { get; }

    internal AgendaOperationData Data { get; }

    internal AgendaOperation Changed(AgendaOperationData data) =>
        AgendaOperations.Create(What, data);
}
