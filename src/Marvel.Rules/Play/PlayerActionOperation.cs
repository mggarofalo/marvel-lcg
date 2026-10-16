using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the PlayerAction procedure.</summary>
public sealed record PlayerActionOperation(PlayerAction Action) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.TurnAction;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PlayerAction;
}
