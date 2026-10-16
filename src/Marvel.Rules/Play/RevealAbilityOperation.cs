using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the RevealAbility procedure.</summary>
public sealed record RevealAbilityOperation(RevealAbilityKind Kind,
    Occurrence? Origin = null, IReadOnlyList<PendingAbility>? Abilities = null,
    IReadOnlyList<int>? Candidates = null, PendingAbility? Completed = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        RevealAbilityKind.ChooseRevealAbility => Steps.ChooseRevealAbility,
        RevealAbilityKind.ResumeRevealAbility => Steps.ResumeRevealAbility,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Reveal;
}
