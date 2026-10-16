using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the AttackStage procedure.</summary>
public sealed record AttackStageOperation(AttackStageKind Kind) : AgendaOperation
{
    internal DamageReplacement? Replacement { get; set; }

    /// <inheritdoc/>
    public override string What => Kind switch
    {
        AttackStageKind.GiveBoostCard => Steps.GiveBoostCard,
        AttackStageKind.DeclareDefender => Steps.DeclareDefender,
        AttackStageKind.FlipBoostCards => Steps.FlipBoostCards,
        AttackStageKind.CalculateAttackDamage => Steps.CalculateAttackDamage,
        AttackStageKind.DealAttackDamage => Steps.DealAttackDamage,
        AttackStageKind.NextAttackTarget => Steps.NextAttackTarget,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
