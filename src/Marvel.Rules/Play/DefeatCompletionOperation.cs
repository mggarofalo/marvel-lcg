using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the DefeatCompletion procedure.</summary>
public sealed record DefeatCompletionOperation(DefeatCompletionKind Kind,
    string Trigger, int Incarnation) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        DefeatCompletionKind.FinalizeCharacterDefeat => Steps.FinalizeCharacterDefeat,
        DefeatCompletionKind.FinalizeSchemeDefeat => Steps.FinalizeSchemeDefeat,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Defeat;
}
