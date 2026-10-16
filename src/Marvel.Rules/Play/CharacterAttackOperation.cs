using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the CharacterAttack procedure.</summary>
public sealed record CharacterAttackOperation(CharacterAttack Attack) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.CharacterAttacks;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
