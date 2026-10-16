using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the AttackAftermath procedure.</summary>
public sealed record AttackAftermathOperation(int Attacker, int Source, string Trigger, CardKind TargetKind,
    long Excess, bool Overkill, bool CanRetaliate) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.FinishAttackDamage;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
