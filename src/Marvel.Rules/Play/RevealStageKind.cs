namespace Marvel.Rules.Play;

/// <summary>Closed stages of one rules procedure.</summary>
public enum RevealStageKind
{
    /// <summary>DealEncounterCards procedure stage.</summary>
    DealEncounterCards,
    /// <summary>RevealEncounterCards procedure stage.</summary>
    RevealEncounterCards,
    /// <summary>RevealEncounterCard procedure stage.</summary>
    RevealEncounterCard,
    /// <summary>DiscardRevealedTreachery procedure stage.</summary>
    DiscardRevealedTreachery,
    /// <summary>FinalizeAllyEntry procedure stage.</summary>
    FinalizeAllyEntry,
}
