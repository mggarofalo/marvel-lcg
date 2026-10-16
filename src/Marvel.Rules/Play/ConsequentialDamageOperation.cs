using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ConsequentialDamage procedure.</summary>
public sealed record ConsequentialDamageOperation(ConsequentialDamageKind Kind,
    int Character = -1) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        ConsequentialDamageKind.AllyConsequentialDamage => Steps.AllyConsequentialDamage,
        ConsequentialDamageKind.AllyThwartConsequentialDamage => Steps.AllyThwartConsequentialDamage,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
