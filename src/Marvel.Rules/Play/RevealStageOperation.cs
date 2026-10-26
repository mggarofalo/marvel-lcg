using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the RevealStage procedure.</summary>
public sealed record RevealStageOperation(RevealStageKind Kind, EncounterRevealOrigin? Origin = null) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        RevealStageKind.DealEncounterCards => Steps.DealEncounterCards,
        RevealStageKind.RevealEncounterCards => Steps.RevealEncounterCards,
        RevealStageKind.RevealEncounterCard => Steps.RevealEncounterCard,
        RevealStageKind.DiscardRevealedTreachery => Steps.DiscardRevealedTreachery,
        RevealStageKind.FinalizeAllyEntry => Steps.FinalizeAllyEntry,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Reveal;
}
