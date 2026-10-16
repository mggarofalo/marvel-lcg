using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the EnemyScheme procedure.</summary>
public sealed record EnemySchemeOperation() : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.Scheme;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Threat;
}
