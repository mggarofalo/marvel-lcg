using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the DefeatAbility procedure.</summary>
public sealed record DefeatAbilityOperation(DefeatAbilityKind Kind,
    DefeatCause Cause, DefeatWindow Window) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        DefeatAbilityKind.ChooseWouldBeDefeated => Steps.ChooseWouldBeDefeated,
        DefeatAbilityKind.ResumeWouldBeDefeated => Steps.ResumeWouldBeDefeated,
        DefeatAbilityKind.ChooseCardDefeatedAbility => Steps.ChooseCardDefeatedAbility,
        DefeatAbilityKind.ResumeCardDefeatedAbility => Steps.ResumeCardDefeatedAbility,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Defeat;
}
