using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ConsequentialDamage procedure.</summary>
public sealed record ConsequentialDamageOperation(ConsequentialDamageKind Kind,
    int Character = -1) : AgendaOperation
{
    /// <summary>The stat substituted for this exact ally thwart.</summary>
    public bool UsesAttack { get; init; }

    /// <summary>The exact performer copy when captured by the initiating power.</summary>
    public int? PerformerIncarnation { get; init; }

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
