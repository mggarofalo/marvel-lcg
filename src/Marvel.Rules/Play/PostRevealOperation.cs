using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the PostReveal procedure.</summary>
public sealed record PostRevealOperation(Occurrence Origin) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.ChoosePostRevealAbility;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Reveal;
}
