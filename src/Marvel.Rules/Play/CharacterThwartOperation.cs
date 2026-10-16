using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the CharacterThwart procedure.</summary>
public sealed record CharacterThwartOperation(CharacterThwart Thwart) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.CharacterThwarts;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
